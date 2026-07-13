using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LMS.BLL.Services
{
    public class VideoEncoderBackgroundService : BackgroundService
    {
        private readonly VideoProcessingChannel _channel;
        private readonly ILogger<VideoEncoderBackgroundService> _logger;

        public VideoEncoderBackgroundService(VideoProcessingChannel channel, ILogger<VideoEncoderBackgroundService> logger)
        {
            _channel = channel;
            _logger = logger;
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
    }
}
