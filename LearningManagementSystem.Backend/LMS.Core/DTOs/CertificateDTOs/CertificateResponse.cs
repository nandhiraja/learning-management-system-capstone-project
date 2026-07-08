using System;

namespace LMS.Core.DTOs
{
    public class CertificateResponse
    {
        public int Id { get; set; }
        public DateTime IssuedDate { get; set; }
        public string CertificateUrl { get; set; } = null!;

        public Guid UserGuid { get; set; }
        public string UserFullName { get; set; } = null!;
        public string UserEmail { get; set; } = null!;

        public Guid CourseGuid { get; set; }
        public string CourseTitle { get; set; } = null!;
        public string InstructorName { get; set; } = null!;
        public string VerificationId { get; set; } = null!;
    }
}
