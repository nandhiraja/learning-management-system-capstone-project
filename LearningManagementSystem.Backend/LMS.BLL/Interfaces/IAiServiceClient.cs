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

    public class StudyPlanResponseDto
    {
        [JsonPropertyName("study_plan_markdown")]
        public string StudyPlanMarkdown { get; set; } = null!;
    }

    public class StudyPlanRequestDto
    {
        public string QuizTitle { get; set; } = null!;
        public List<StudyPlanWrongQuestionDto> WrongQuestions { get; set; } = new List<StudyPlanWrongQuestionDto>();
        public List<string> CourseOutline { get; set; } = new List<string>();
        public string ContextTranscripts { get; set; } = null!;
        public int AttemptsUsed { get; set; } = 1;
        public int MaxAttempts { get; set; } = 3;
    }

    public class StudyPlanWrongQuestionDto
    {
        public string QuestionText { get; set; } = null!;
        public string StudentAnswerText { get; set; } = null!;
        public string CorrectAnswerText { get; set; } = null!;
    }

    public interface IAiServiceClient
    {
        Task<List<TranscriptSegmentDto>> TranscribeAudioAsync(string audioFilePath);
        Task<AiServiceEnvelope<AskQuestionResponseDto>> AskQuestionAsync(string context, string question, string role);
        Task<List<PdfPageTextDto>> ExtractPdfTextAsync(string pdfFilePath);
        Task<AiServiceEnvelope<StudyPlanResponseDto>> GenerateStudyPlanAsync(StudyPlanRequestDto request);
        Task<AiServiceEnvelope<List<GeneratedQuizQuestionDto>>> GenerateQuizAsync(string lectureTranscript, string lectureTitle, int numQuestions, List<string> existingQuestions);
    }

    // Quiz Generator DTOs
    public class GeneratedQuizQuestionDto
    {
        [JsonPropertyName("questionText")]
        public string QuestionText { get; set; } = null!;

        [JsonPropertyName("options")]
        public List<GeneratedOptionDto> Options { get; set; } = new List<GeneratedOptionDto>();
    }

    public class GeneratedOptionDto
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = null!;

        [JsonPropertyName("isCorrect")]
        public bool IsCorrect { get; set; }
    }

    public class GenerateQuizRequestDto
    {
        public int NumQuestions { get; set; } = 5;
        public List<string> ExistingQuestions { get; set; } = new List<string>();
    }
}

