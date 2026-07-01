using System;

namespace LMS.Core.Models
{
    public class DiscussionReplyLike
    {
        public int Id { get; set; }
        public int ReplyId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public DiscussionReply Reply { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
