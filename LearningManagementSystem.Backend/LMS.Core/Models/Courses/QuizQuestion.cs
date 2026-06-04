namespace LMS.Core.Models
{
    public class QuizQuestion
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public string QuestionText { get; set; } = null!;

        // Navigation properties
        public Quiz Quiz { get; set; }=null!;
        public IEnumerable<QuizOption> Options { get; set; } = new List<QuizOption>();
    }
}