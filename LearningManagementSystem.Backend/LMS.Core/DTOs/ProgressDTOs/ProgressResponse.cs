namespace LMS.Core.DTOs
{
    public class ProgressUpdateRequest
    {
        public int LectureId { get; set; }
        public int WatchedSeconds { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class ProgressResponse
    {
        public int CompletedLectures { get; set; }
        public int TotalLectures { get; set; }
        public double Percentage { get; set; }
    }
}
