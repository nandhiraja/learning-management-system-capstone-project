namespace LMS.Core.Models
{
   public class Certificate
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CourseId { get; set; }
        public int EnrollmentId { get; set; }
        public string CertificateUrl { get; set; }= null!;
        public DateTime IssuedDate { get; set; }


        // Navigation properties
        public User User { get; set; } = null!;
        public Course Course { get; set; }= null!;
        public Enrollment Enrollment { get; set; }= null!;
    }
}