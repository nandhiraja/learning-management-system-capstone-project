using System;

namespace LMS.Core.Models
{
    public class QuizProgress
    {
        public int UserId { get; set; }
        public int QuizId { get; set; }
        
        public int AttemptsUsed { get; set; }
        public int HighestScore { get; set; }
        public bool IsPassed { get; set; }
        public DateTime LastAttemptDate { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public Quiz Quiz { get; set; } = null!;
    }
}
