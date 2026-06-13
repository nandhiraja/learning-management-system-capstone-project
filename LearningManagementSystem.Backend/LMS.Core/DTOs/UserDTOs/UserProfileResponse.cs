using System;

namespace LMS.Core.DTOs
{
    public class UserProfileResponse
    {
        public Guid ExternalId { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNo { get; set; } = null!;
        public string? ProfilePictureUrl { get; set; }
        public string Role { get; set; } = null!;
    }
}
