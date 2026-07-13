namespace LMS.Core.DTOs
{
    public class QuizProgressResponse
    {
        public int QuizId { get; set; }
        public int AttemptsUsed { get; set; }
        public int MaxAttempts { get; set; }
        public int HighestScore { get; set; }
        public int PassScore { get; set; }
        public bool IsPassed { get; set; }
    }
}
