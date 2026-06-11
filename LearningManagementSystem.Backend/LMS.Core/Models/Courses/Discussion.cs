using System;
using System.Collections.Generic;

namespace LMS.Core.Models
{
    public class Discussion
    {
        public int Id { get; set; }
        public Guid ExternalId { get; set; } = Guid.NewGuid();
        public int CourseId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Course Course { get; set; } = null!;
        public User User { get; set; } = null!;
        public ICollection<DiscussionReply> Replies { get; set; } = new List<DiscussionReply>();
    }
}
