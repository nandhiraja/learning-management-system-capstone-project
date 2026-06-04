namespace LMS.Core.Models
{
    public class Language
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        // Navigation Property
        public IEnumerable<Course> Courses { get; set; } = new List<Course>();
    }
}