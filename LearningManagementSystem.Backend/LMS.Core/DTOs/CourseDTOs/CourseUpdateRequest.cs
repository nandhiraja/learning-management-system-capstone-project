using System.ComponentModel.DataAnnotations;

namespace LMS.Core.DTOs
{
    public class CourseUpdateRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Title { get; set; } = null!;

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = null!;

        [Required]
        [Range(1, int.MaxValue)]
        public int CategoryId { get; set; }

        [Required]
        [Range(0.0, 100000.0)]
        public decimal Price { get; set; }

        [Required]
        [StringLength(50)]
        public string Language { get; set; } = null!;
    }
}
