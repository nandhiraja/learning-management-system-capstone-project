using System;

namespace LMS.Core.DTOs
{
    public class DiscussionResponse
    {
        public Guid ExternalId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public UserProfileResponse User { get; set; } = null!;
        public int RepliesCount { get; set; }
        public int LectureId { get; set; }
        public string? LectureTitle { get; set; }
    }
}
