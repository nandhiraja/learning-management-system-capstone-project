using System.ComponentModel.DataAnnotations;

namespace LMS.Core.DTOs
{
    public class CourseSectionRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Title { get; set; } = null!;

        [Required]
        [Range(1, 1000)]
        public int Order { get; set; }
    }
}
