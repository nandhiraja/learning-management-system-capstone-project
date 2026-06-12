using System.ComponentModel.DataAnnotations;

namespace LMS.Core.DTOs
{
    public class DiscussionReplyCreateRequest
    {
        [Required]
        [StringLength(5000, MinimumLength = 1, ErrorMessage = "Content must be between 1 and 5000 characters.")]
        public string Content { get; set; } = null!;
    }
}
