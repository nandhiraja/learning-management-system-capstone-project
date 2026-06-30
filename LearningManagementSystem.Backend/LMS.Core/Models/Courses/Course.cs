using LMS.Core.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace LMS.Core.Models
{
    public class Course
    {
        public int Id { get; set; }
        public Guid ExternalId { get; set; } = Guid.NewGuid();
        public int InstructorId { get; set; }
        public int CategoryId { get; set; }
        public  int LanguageId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; }  = String.Empty;
        public string? ThumbnailUrl { get; set; }
        public int DurationInMinutes { get; set; }
        public decimal Price { get; set; }
        public int DiscountPercentage { get; set; }

        public CourseStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int? OriginalCourseId { get; set; }
        public Course? OriginalCourse { get; set; }


        // Navigation property
        public User Instructor { get; set; } = null!;
        public Category Category { get; set; } = null!;
        public Language Language { get; set; } = null!;
        public IEnumerable<CourseSection> Sections { get; set; } = new List<CourseSection>();
        public IEnumerable<CourseReview> CourseReviews { get; set; } = new List<CourseReview>();
        public IEnumerable<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public IEnumerable<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}