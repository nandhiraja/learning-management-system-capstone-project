
namespace LMS.Core.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        //Navigation property
        public IEnumerable<Course> Courses { get; set; } = new List<Course>();

    }
}