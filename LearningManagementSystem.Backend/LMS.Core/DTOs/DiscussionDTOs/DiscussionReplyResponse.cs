using System;

namespace LMS.Core.DTOs
{
    public class DiscussionReplyResponse
    {
        public Guid ExternalId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public UserProfileResponse User { get; set; } = null!;
        public bool IsPinned { get; set; }
        public int LikesCount { get; set; }
        public bool IsInstructorReply { get; set; }
        public bool IsAuthorReply { get; set; }
    }
}
