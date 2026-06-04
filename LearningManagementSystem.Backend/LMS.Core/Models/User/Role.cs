namespace LMS.Core.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        // Navigation property  
        public IEnumerable<User> Users { get; set; } = new List<User>();
    }
}   
