using System.Collections.Generic;

namespace LMS.Core.DTOs
{
    public class QuizQuestionRequest
    {
        public string QuestionText { get; set; } = null!;
        public IEnumerable<QuizOptionRequest> Options { get; set; } = new List<QuizOptionRequest>();
    }

    public class QuizOptionRequest
    {
        public string OptionText { get; set; } = null!;
        public bool IsCorrect { get; set; }
    }
}
