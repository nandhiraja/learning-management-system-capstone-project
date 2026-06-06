using System.Collections.Generic;

namespace LMS.Core.DTOs
{
    public class QuizResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public int PassScore { get; set; }
        public IEnumerable<QuizQuestionResponse> Questions { get; set; } = new List<QuizQuestionResponse>();
    }

    public class QuizQuestionResponse
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = null!;
        public IEnumerable<QuizOptionResponse> Options { get; set; } = new List<QuizOptionResponse>();
    }

    public class QuizOptionResponse
    {
        public int Id { get; set; }
        public string OptionText { get; set; } = null!;
    }
}
