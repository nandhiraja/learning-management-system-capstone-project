using LMS.Core.Enums;

namespace LMS.Core.Models
{
    public class LectureProgress
    {
        public int Id { get; set; }
        public int LectureId { get; set; }
        public int EnrollmentId { get; set; }
        public LectureStatus Status { get; set; }
        public int WatchedSeconds { get; set; }
        public DateTime? LastAccessedAt { get; set; }

        // Navigation properties
        public Lecture Lecture { get; set; } = null!;
        public Enrollment Enrollment { get; set; } = null!;
    }
}