using System.ComponentModel.DataAnnotations;

namespace LMS.Core.DTOs
{
    public class UserEditRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "FirstName must be between 2 and 50 characters.")]
        public string FirstName { get; set; } = null!;

        [Required]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "LastName must be between 1 and 50 characters.")]
        public string LastName { get; set; } = null!;

        [Required]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(20)]
        public string PhoneNo { get; set; } = null!;

        [StringLength(1000)]
        public string? ProfilePictureUrl { get; set; }
    }
}