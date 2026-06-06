using System.Collections.Generic;

namespace LMS.Core.DTOs
{
    public class CourseSectionResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public int Order { get; set; }
        public IEnumerable<LectureResponse> Lectures { get; set; } = new List<LectureResponse>();
    }
}
