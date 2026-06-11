using System;

namespace LMS.Core.Models
{
    public class UserRefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = null!;
        public DateTime ExpiryTime { get; set; }
        public int UserId { get; set; }

        // Navigation property
        public User User { get; set; } = null!;
    }
}
