using System;
using System.Collections.Generic;

namespace LMS.Core.DTOs
{
    public class CourseResponse
    {
        public int Id { get; set; }
        public Guid ExternalId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Price { get; set; }
        public string Language { get; set; } = null!;
        public string? ThumbnailUrl { get; set; }
        public double Rating { get; set; }
        public int StudentsCount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public UserProfileResponse Instructor { get; set; } = null!;
        public IEnumerable<CourseSectionResponse> Sections { get; set; } = new List<CourseSectionResponse>();
        public Guid? OriginalCourseExternalId { get; set; }
        public CourseResponse? OriginalCourseDetails { get; set; }
    }
}
