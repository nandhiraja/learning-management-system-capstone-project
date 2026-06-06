namespace LMS.Core.DTOs
{
    public class ReviewRequest
    {
        public int Rating { get; set; } // 1 to 5
        public string? Comment { get; set; }
    }

    public class ReviewResponse
    {
        public int Id { get; set; }
        public string UserName { get; set; } = null!;
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
