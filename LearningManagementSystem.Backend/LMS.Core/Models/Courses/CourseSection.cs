namespace LMS.Core.Models
{
    public class CourseSection
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int CourseId { get; set; }
        public int Order { get; set; }
      

        public DateTime CreatedAt { get; set; } 
        public DateTime UpdatedAt { get; set; }

        // Navigation property
        public Course Course { get; set; } = null!;
        public List<Lecture> Lectures { get; set; } = new List<Lecture>();
    }
}