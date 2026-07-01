using System;

namespace LMS.Core.Models
{
    public class DiscussionReply
    {
        public int Id { get; set; }
        public Guid ExternalId { get; set; } = Guid.NewGuid();
        public int DiscussionId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; } = null!;
        public bool IsPinned { get; set; } = false;
        public int LikesCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Discussion Discussion { get; set; } = null!;
        public User User { get; set; } = null!;
        public ICollection<DiscussionReplyLike> Likes { get; set; } = new List<DiscussionReplyLike>();
    }
}
