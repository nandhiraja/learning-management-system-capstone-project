using System.ComponentModel.DataAnnotations;

namespace LMS.Core.DTOs
{
    public class ReviewRequest
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; } // 1 to 5

        [Required]
        [StringLength(1000)]
        public string? Comment { get; set; }
    }

    public class ReviewResponse
    {
        public int Id { get; set; }
        public string UserName { get; set; } = null!;
        public string UserFullName { get; set; } = null!;
        public string? ProfilePictureUrl { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
