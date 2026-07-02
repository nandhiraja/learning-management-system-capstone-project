using System;
using System.Collections.Generic;

namespace LMS.Core.DTOs
{
    public class DiscussionDetailResponse
    {
        public Guid ExternalId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public UserProfileResponse User { get; set; } = null!;
        public int LectureId { get; set; }
        public string? LectureTitle { get; set; }
        public bool IsInstructorThread { get; set; }
        public IEnumerable<DiscussionReplyResponse> Replies { get; set; } = new List<DiscussionReplyResponse>();
    }
}
