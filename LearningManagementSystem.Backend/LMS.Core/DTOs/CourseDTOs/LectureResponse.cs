namespace LMS.Core.DTOs
{
    public class LectureResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string ContentUrl { get; set; } = null!;
        public int DurationInMinutes { get; set; }
        public string ContentType { get; set; } = null!;
        public int? QuizId { get; set; }
    }
}
