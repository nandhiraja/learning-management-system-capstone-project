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
                var result = JsonSerializer.Deserialize<AiServiceEnvelope<TranscriptResponse>>(responseBody, options);

                return result?.Data?.Segments ?? new List<TranscriptSegmentDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Python transcription service.");
                throw;
            }
        }

        public async Task<AiServiceEnvelope<AskQuestionResponseDto>> AskQuestionAsync(string context, string question, string role)
        {
            var baseUrl = _configuration["AiService:BaseUrl"] ?? "http://localhost:8000";
            string path = role.ToLower() == "instructor" ? "instructor/ask" : "student/ask";
            var url = $"{baseUrl.TrimEnd('/')}/api/ai/{path}";

            _logger.LogInformation("Sending Ask AI request to Python service: {Url} for role {Role}", url, role);

            var requestBody = new { context = context, question = question };
            var jsonString = JsonSerializer.Serialize(requestBody);
            using var requestContent = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, requestContent);
                var responseBody = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var result = JsonSerializer.Deserialize<AiServiceEnvelope<AskQuestionResponseDto>>(responseBody, options);

                if (result != null)
                {
                    return result;
                }

                return new AiServiceEnvelope<AskQuestionResponseDto>
                {
                    Success = false,
                    Error = new AiServiceError
                    {
                        Code = "INTERNAL_ERROR",
                        Message = $"Failed to parse response from AI service: {response.StatusCode} - {responseBody}"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Python Ask AI service.");
                return new AiServiceEnvelope<AskQuestionResponseDto>
                {
                    Success = false,
                    Error = new AiServiceError
                    {
                        Code = "LLM_UNAVAILABLE",
                        Message = "Could not reach the AI service: " + ex.Message
                    }
                };
            }
        }

        public async Task<List<PdfPageTextDto>> ExtractPdfTextAsync(string pdfFilePath)
        {
            if (!File.Exists(pdfFilePath))
            {
                throw new FileNotFoundException("PDF file for text extraction not found.", pdfFilePath);
            }

            var baseUrl = _configuration["AiService:BaseUrl"] ?? "http://localhost:8000";
            var url = $"{baseUrl.TrimEnd('/')}/api/ai/pdf/extract";

            _logger.LogInformation("Sending PDF text extraction request to Python service: {Url}", url);

            using var form = new MultipartFormDataContent();
            using var fileStream = new FileStream(pdfFilePath, FileMode.Open, FileAccess.Read);
            using var streamContent = new StreamContent(fileStream);
            
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            form.Add(streamContent, "file", Path.GetFileName(pdfFilePath));

            try
            {
                var response = await _httpClient.PostAsync(url, form);
                if (!response.IsSuccessStatusCode)
                {
                    var errBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Python AI Service PDF extraction failed with status {Status}. Error: {Error}", response.StatusCode, errBody);
                    throw new Exception($"PDF text extraction failed: {response.StatusCode} - {errBody}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var result = JsonSerializer.Deserialize<AiServiceEnvelope<PdfExtractResponseDto>>(responseBody, options);

                return result?.Data?.Pages ?? new List<PdfPageTextDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Python PDF extraction service.");
                throw;
            }
        }

        private class TranscriptResponse
        {
            public List<TranscriptSegmentDto> Segments { get; set; } = new List<TranscriptSegmentDto>();
        }
    }
}
