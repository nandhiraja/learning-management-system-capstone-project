namespace LMS.Core.DTOs
{
    public class LectureRequest
    {
        public string Title { get; set; } = null!;
        public string ContentUrl { get; set; } = null!;
        public int DurationInMinutes { get; set; }
        public string ContentType { get; set; } = null!; // maps to ContentType enum
    }
}
