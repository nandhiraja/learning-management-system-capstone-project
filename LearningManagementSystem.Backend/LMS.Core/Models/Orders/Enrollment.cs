
using LMS.Core.Enums;

namespace LMS.Core.Models
{
   public class Enrollment
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public int UserId { get; set; }
        public int OrderItemId { get; set; }
        public EnrollmentStatus Status { get; set; }
        public DateTime EnrolledAt { get; set; }

        // Navigation properties
        public Course Course { get; set; } = null!;
        public OrderItem OrderItem { get; set; } = null!;
        public User User { get; set; } = null!;
        public Certificate? Certificate { get; set; }
        public IEnumerable<LectureProgress> LectureProgresses { get; set; } = new List<LectureProgress>();

    }
}