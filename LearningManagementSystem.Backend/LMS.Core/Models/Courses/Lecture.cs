using LMS.Core.Enums;
namespace LMS.Core.Models
{
    public class Lecture
    {
        public int Id { get; set; }
        public int CourseSectionId { get; set; }
        public string Title { get; set; } = null!;
        public string ContentUrl { get; set; } = null!;
        public ContentType ContentType { get; set; }
        public int DurationInMinutes { get; set; }
        public LectureStatus Status { get; set; }

        // Navigation properties    
        public CourseSection CourseSection { get; set; } = null!;
        public IEnumerable<Quiz> Quizzes { get; set; } = new List<Quiz>();
        public IEnumerable<LectureProgress> LectureProgresses { get; set; } = new List<LectureProgress>();
    }
}