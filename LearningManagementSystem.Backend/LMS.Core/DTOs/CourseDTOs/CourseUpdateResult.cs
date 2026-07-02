using System;

namespace LMS.Core.DTOs
{
    public class CourseUpdateResult
    {
        public bool Success { get; set; }
        public Guid UpdatedCourseGuid { get; set; }
    }
}
