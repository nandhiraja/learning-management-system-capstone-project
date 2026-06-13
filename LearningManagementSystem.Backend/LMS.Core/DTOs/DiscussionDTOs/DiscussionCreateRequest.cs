using System.ComponentModel.DataAnnotations;

namespace LMS.Core.DTOs
{
    public class DiscussionCreateRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters.")]
        public string Title { get; set; } = null!;

        [Required]
        [StringLength(5000, MinimumLength = 3, ErrorMessage = "Content must be between 3 and 5000 characters.")]
        public string Content { get; set; } = null!;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "LectureId must be a valid ID.")]
        public int LectureId { get; set; }
    }
}
