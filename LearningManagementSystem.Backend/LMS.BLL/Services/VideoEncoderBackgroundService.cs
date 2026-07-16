using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using LMS.BLL.Interfaces;
using LMS.DAL.Data;
using LMS.Core.Models;
using System.Linq;

namespace LMS.BLL.Services
{
    public class VideoEncoderBackgroundService : BackgroundService
    {
        private readonly VideoProcessingChannel _channel;
        private readonly ILogger<VideoEncoderBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public VideoEncoderBackgroundService(VideoProcessingChannel channel, ILogger<VideoEncoderBackgroundService> logger, IServiceProvider serviceProvider)
        {
            _channel = channel;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Video Encoder Background Service is starting.");

            await foreach (var filePath in _channel.ReadAllAsync(stoppingToken))
            {
                _logger.LogInformation("Picked up video for encoding: {FilePath}", filePath);
                try
                {
                    await ProcessVideoAsync(filePath, stoppingToken);
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Error processing video {FilePath}", filePath);
                }
            }
        }

        private async Task ProcessVideoAsync(string relativeFilePath, CancellationToken cancellationToken)
        {
            var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), relativeFilePath);
            if (!File.Exists(absolutePath))
            {
                _logger.LogWarning("File not found for processing: {Path}", absolutePath);
                return;
            }

            // Create HLS output directory based on the file name without extension
            var directory = Path.GetDirectoryName(absolutePath);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(absolutePath);
            var hlsDirectory = Path.Combine(directory!, fileNameWithoutExtension);

            if (!Directory.Exists(hlsDirectory))
            {
                Directory.CreateDirectory(hlsDirectory);
            }

            var playlistPath = Path.Combine(hlsDirectory, "playlist.m3u8");
            var segmentPattern = Path.Combine(hlsDirectory, "segment_%03d.ts");

            var arguments = $"-y -i \"{absolutePath}\" -codec:v libx264 -codec:a aac -hls_time 10 -hls_playlist_type vod -hls_segment_filename \"{segmentPattern}\" \"{playlistPath}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                // We could read output here, but for simplicity we'll just wait
                await process.WaitForExitAsync(cancellationToken);
                
                if (process.ExitCode == 0)
                {
                    _logger.LogInformation("Successfully encoded video to HLS: {PlaylistPath}", playlistPath);
                    
                    // Call audio extraction and transcription pipeline
                    await ExtractAudioAndTranscribeAsync(absolutePath, hlsDirectory, relativeFilePath, cancellationToken);

                    // Automatically delete the original .mp4 file to save space
                    try
                    {
                        File.Delete(absolutePath);
                        _logger.LogInformation("Deleted original video file: {AbsolutePath}", absolutePath);
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete original video file: {AbsolutePath}", absolutePath);
                    }
                }
                else
                {
                    var error = await process.StandardError.ReadToEndAsync(cancellationToken);
                    _logger.LogError("FFmpeg failed with exit code {ExitCode}. Error: {Error}", process.ExitCode, error);
                }
            }
            else
            {
                _logger.LogError("Failed to start FFmpeg process.");
            }
        }

        private async Task ExtractAudioAndTranscribeAsync(string absoluteVideoPath, string hlsDirectory, string relativeFilePath, CancellationToken cancellationToken)
        {
            var audioPath = Path.Combine(hlsDirectory, "audio.mp3");
            var audioArguments = $"-y -i \"{absoluteVideoPath}\" -q:a 0 -map a \"{audioPath}\"";

            _logger.LogInformation("Extracting audio from video: {AudioPath}", audioPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = audioArguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var audioProcess = Process.Start(startInfo);
            if (audioProcess != null)
            {
                await audioProcess.WaitForExitAsync(cancellationToken);
                if (audioProcess.ExitCode == 0)
                {
                    _logger.LogInformation("Successfully extracted audio for transcription.");
                    try
                    {
                        var predictedUrl = relativeFilePath.Replace(".mp4", "/playlist.m3u8")
                                                           .Replace(".mov", "/playlist.m3u8")
                                                           .Replace(".avi", "/playlist.m3u8")
                                                           .Replace(".mkv", "/playlist.m3u8")
                                                           .Replace(".webm", "/playlist.m3u8");

                        using var scope = _serviceProvider.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<LMSDBContext>();
                        var aiClient = scope.ServiceProvider.GetRequiredService<IAiServiceClient>();

                        Lecture? lecture = null;
                        int retryCount = 0;
                        while (lecture == null && retryCount < 6)
                        {
                            lecture = dbContext.Lectures.FirstOrDefault(l => l.ContentUrl == predictedUrl);
                            if (lecture == null)
                            {
                                retryCount++;
                                _logger.LogInformation("Lecture with ContentUrl {Url} not found in DB yet. Retry {RetryCount}/6 in 5s...", predictedUrl, retryCount);
                                await Task.Delay(5000, cancellationToken);
                                dbContext.ChangeTracker.Clear(); // Clear tracking to fetch fresh data from database
                            }
                        }

                        if (lecture != null)
                        {
                            _logger.LogInformation("Found matching lecture {LectureId} for transcription after {RetryCount} retries.", lecture.Id, retryCount);
                            var segments = await aiClient.TranscribeAudioAsync(audioPath);
                            
                            if (segments != null && segments.Count > 0)
                            {
                                var existing = dbContext.LectureTranscripts.Where(t => t.LectureId == lecture.Id);
                                dbContext.LectureTranscripts.RemoveRange(existing);

                                foreach (var seg in segments)
                                {
                                    dbContext.LectureTranscripts.Add(new LectureTranscript
                                    {
                                        LectureId = lecture.Id,
                                        StartTime = seg.StartTime,
                                        EndTime = seg.EndTime,
                                        Text = seg.Text
                                    });
                                }
                                await dbContext.SaveChangesAsync(cancellationToken);
                                _logger.LogInformation("Saved {Count} transcript segments for Lecture {LectureId}.", segments.Count, lecture.Id);
                            }
                            else
                            {
                                _logger.LogWarning("AI Service returned no transcript segments for video Lecture {LectureId}.", lecture.Id);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("No matching Lecture found in DB with ContentUrl: {Url}", predictedUrl);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex, "Failed during transcription call or database save.");
                    }
                    finally
                    {
                        try
                        {
                            if (File.Exists(audioPath))
                            {
                                File.Delete(audioPath);
                                _logger.LogInformation("Cleaned up temporary audio file: {AudioPath}", audioPath);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            _logger.LogError(ex, "Failed to delete temporary audio file: {AudioPath}", audioPath);
                        }
                    }
                }
                else
                {
                    var error = await audioProcess.StandardError.ReadToEndAsync(cancellationToken);
                    _logger.LogError("Audio extraction failed with exit code {ExitCode}. Error: {Error}", audioProcess.ExitCode, error);
                }
            }
        }
    }
}
