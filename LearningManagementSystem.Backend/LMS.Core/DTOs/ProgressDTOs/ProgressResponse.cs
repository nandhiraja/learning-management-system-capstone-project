using System.ComponentModel.DataAnnotations;

namespace LMS.Core.DTOs
{
    public class ProgressUpdateRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "LectureId must be a positive integer.")]
        public int LectureId { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "WatchedSeconds must be a non-negative integer.")]
        public int WatchedSeconds { get; set; }

        [Required]
        public bool IsCompleted { get; set; }
    }

    public class ProgressResponse
    {
        public int CompletedLectures { get; set; }
        public int TotalLectures { get; set; }
        public double Percentage { get; set; }
        public IEnumerable<int> CompletedLectureIds { get; set; } = new List<int>();
    }
}
