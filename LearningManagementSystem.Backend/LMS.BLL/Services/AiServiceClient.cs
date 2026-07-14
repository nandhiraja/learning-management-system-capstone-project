using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using LMS.BLL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LMS.BLL.Services
{
    public class AiServiceClient : IAiServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiServiceClient> _logger;

        public AiServiceClient(HttpClient httpClient, IConfiguration configuration, ILogger<AiServiceClient> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<TranscriptSegmentDto>> TranscribeAudioAsync(string audioFilePath)
        {
            if (!File.Exists(audioFilePath))
            {
                throw new FileNotFoundException("Audio file for transcription not found.", audioFilePath);
            }

            var baseUrl = _configuration["AiService:BaseUrl"] ?? "http://localhost:8000";
            var url = $"{baseUrl.TrimEnd('/')}/api/ai/transcribe";

            _logger.LogInformation("Sending transcription request to Python service: {Url}", url);

            using var form = new MultipartFormDataContent();
            using var fileStream = new FileStream(audioFilePath, FileMode.Open, FileAccess.Read);
            using var streamContent = new StreamContent(fileStream);
            
            // Set content type manually to avoid API model errors
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/mpeg");
            form.Add(streamContent, "file", Path.GetFileName(audioFilePath));

            try
            {
                var response = await _httpClient.PostAsync(url, form);
                if (!response.IsSuccessStatusCode)
                {
                    var errBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Python AI Service failed with status {Status}. Error: {Error}", response.StatusCode, errBody);
                    throw new Exception($"Transcription service failed: {response.StatusCode} - {errBody}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var result = JsonSerializer.Deserialize<TranscriptResponse>(responseBody, options);

                return result?.Segments ?? new List<TranscriptSegmentDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Python transcription service.");
                throw;
            }
        }

        private class TranscriptResponse
        {
            public List<TranscriptSegmentDto> Segments { get; set; } = new List<TranscriptSegmentDto>();
        }
    }
}
