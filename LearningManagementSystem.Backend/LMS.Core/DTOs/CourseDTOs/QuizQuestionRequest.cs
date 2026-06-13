using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LMS.Core.DTOs
{
    public class QuizQuestionRequest
    {
        [Required]
        [StringLength(1000, MinimumLength = 3, ErrorMessage = "QuestionText must be between 3 and 1000 characters.")]
        public string QuestionText { get; set; } = null!;

        [Required]
        public IEnumerable<QuizOptionRequest> Options { get; set; } = new List<QuizOptionRequest>();
    }

    public class QuizOptionRequest
    {
        [Required]
        [StringLength(500, MinimumLength = 1, ErrorMessage = "OptionText must be between 1 and 500 characters.")]
        public string OptionText { get; set; } = null!;

        [Required]
        public bool IsCorrect { get; set; }
    }
}
