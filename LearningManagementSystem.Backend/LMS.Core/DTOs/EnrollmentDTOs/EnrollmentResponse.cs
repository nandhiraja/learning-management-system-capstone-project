using System;

namespace LMS.Core.DTOs
{
    public class EnrollmentResponse
    {
        public int CourseId { get; set; }
        public Guid CourseExternalId { get; set; }
        public string CourseTitle { get; set; } = null!;
        public double Progress { get; set; }
    }
}
