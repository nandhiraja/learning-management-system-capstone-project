using System;

namespace LMS.Core.DTOs.PublicDTOs
{
    public class TopInstructorResponse
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public int TotalStudents { get; set; }
        public int CourseCount { get; set; }
    }
}
