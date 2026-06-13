using System.ComponentModel.DataAnnotations;

namespace LMS.Core.DTOs
{
    public class LectureRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Title { get; set; } = null!;

        [Required]
        [StringLength(1000)]
        public string ContentUrl { get; set; } = null!;

        [Required]
        [Range(1, 10000)]
        public int DurationInMinutes { get; set; }

        [Required]
        [RegularExpression("^(?i)(Video|pdf|ExternalLink|Text|PPT)$", ErrorMessage = "Invalid ContentType. Allowed values: Video, pdf, ExternalLink, Text, PPT")]
        public string ContentType { get; set; } = null!; // maps to ContentType enum
    }
}
