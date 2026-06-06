namespace LMS.Core.DTOs
{
    public class CourseUpdateRequest
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int CategoryId { get; set; }
        public decimal Price { get; set; }
        public string Language { get; set; } = null!;
    }
}
