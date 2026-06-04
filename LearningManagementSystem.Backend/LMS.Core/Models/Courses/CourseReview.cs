namespace LMS.Core.Models
{
    public class CourseReview
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public int UserId { get; set; }
        public int Rating { get; set; } // 1 to 5
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } 
      
      
        // Navigation property
        public User User { get; set; } = null!;
        public Course Course { get; set; } = null!;
    }
}