using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public class TranscriptSegmentDto
    {
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public string Text { get; set; } = null!;
    }

    public class AiServiceEnvelope<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public AiServiceError? Error { get; set; }
    }

    public class AiServiceError
    {
        public string Code { get; set; } = null!;
        public string Message { get; set; } = null!;
    }

    public class AskQuestionResponseDto
    {
        public string Answer { get; set; } = null!;

        [JsonPropertyName("context_truncated")]
        public bool ContextTruncated { get; set; }
    }

    public class PdfPageTextDto
    {
        public int PageNumber { get; set; }
        public string Text { get; set; } = null!;
    }

    public class PdfExtractResponseDto
    {
        public List<PdfPageTextDto> Pages { get; set; } = new List<PdfPageTextDto>();
    }

    public interface IAiServiceClient
    {
        Task<List<TranscriptSegmentDto>> TranscribeAudioAsync(string audioFilePath);
        Task<AiServiceEnvelope<AskQuestionResponseDto>> AskQuestionAsync(string context, string question, string role);
        Task<List<PdfPageTextDto>> ExtractPdfTextAsync(string pdfFilePath);
    }
}

