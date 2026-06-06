using System.Collections.Generic;

namespace LMS.Core.DTOs
{
    public class QuizSubmitRequest
    {
        public IEnumerable<QuizSubmitAnswer> Answers { get; set; } = new List<QuizSubmitAnswer>();
    }

    public class QuizSubmitAnswer
    {
        public int QuestionId { get; set; }
        public int OptionId { get; set; }
    }
}
