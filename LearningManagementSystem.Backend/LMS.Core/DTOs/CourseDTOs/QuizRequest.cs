using System.Collections.Generic;

namespace LMS.Core.DTOs
{
    public class QuizRequest
    {
        public string Title { get; set; } = null!;
        public int PassScore { get; set; }
        public IEnumerable<QuizQuestionRequest> Questions { get; set; } = new List<QuizQuestionRequest>();
    }
}

