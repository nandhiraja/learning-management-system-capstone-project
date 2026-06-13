using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LMS.Core.DTOs
{
    public class QuizRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Title { get; set; } = null!;

        [Required]
        [Range(1, 100)]
        public int PassScore { get; set; }

        [Required]
        public IEnumerable<QuizQuestionRequest> Questions { get; set; } = new List<QuizQuestionRequest>();
    }
}

