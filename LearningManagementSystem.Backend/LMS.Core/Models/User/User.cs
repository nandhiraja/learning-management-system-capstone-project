namespace LMS.Core.Models
{
    public class User
    {
        public int Id { get; set; }
        public string  UserName {get;set;} = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = String.Empty;
        public string Email { get; set; }= null!;
        public string PasswordHash { get; set; }= null!;
        public string? ProfilePictureUrl { get; set; }
        public  int RoleId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation property
        public Role Role { get; set; } = null!;
        public IEnumerable<Course> CreatedCourses { get; set; } = new List<Course>();
        public IEnumerable<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public IEnumerable<CourseReview> Reviews { get; set; } = new List<CourseReview>();
        public IEnumerable<Certificate> Certificates { get; set; } = new List<Certificate>();
        public IEnumerable<Order> Orders { get; set; } = new List<Order>();


    }
}