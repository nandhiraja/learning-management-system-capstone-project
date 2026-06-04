namespace LMS.Core.Models
{
    public class QuizOption
    {
        public int Id { get; set; }
        public int QuizQuestionId { get; set; }
        public string OptionText { get; set; }=null!;
        public bool IsCorrect { get; set; }

        // Navigation property
        public QuizQuestion QuizQuestion { get; set; } = null!;
    }
}