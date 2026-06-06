namespace LMS.Core.DTOs
{
    public class CourseCreateRequest
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int CategoryId { get; set; }
        public string Language { get; set; } = null!;
        public decimal Price { get; set; }
        public string? ThumbnailUrl { get; set; }
    }
}
