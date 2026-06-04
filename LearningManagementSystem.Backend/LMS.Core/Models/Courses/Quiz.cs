using LMS.Core.Enums;

namespace LMS.Core.Models
{
    public class Quiz
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public int? LectureId { get; set; }
        public string QuizName { get; set; } = null!;
        public string? Description { get; set; }
        public int TotalMarks { get; set; }
        public int PassingMarks { get; set; }
        public int MaxAttempts { get; set; }
        public int CurrentAttempt { get; set; }
        public QuizStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }


        // Navigation properties
        public Course Course { get; set; } = null!;
        public Lecture? Lecture { get; set; }
        public IEnumerable<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    }
}