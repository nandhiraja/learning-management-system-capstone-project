using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LMS.Core.DTOs
{
    public class QuizSubmitRequest
    {
        [Required]
        public IEnumerable<QuizSubmitAnswer> Answers { get; set; } = new List<QuizSubmitAnswer>();
    }

    public class QuizSubmitAnswer
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "QuestionId must be a positive integer.")]
        public int QuestionId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "OptionId must be a positive integer.")]
        public int OptionId { get; set; }
    }
}
