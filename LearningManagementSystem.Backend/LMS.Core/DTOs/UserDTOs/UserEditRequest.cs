namespace LMS.Core.DTOs
{
    public class UserEditRequest
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string PhoneNo { get; set; } = null!;
        public string? ProfilePictureUrl { get; set; }
    }
}