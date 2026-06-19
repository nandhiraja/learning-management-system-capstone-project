using System.ComponentModel.DataAnnotations;

namespace LMS.Core.DTOs
{
    public class RegisterRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string FirstName { get; set; } = null!;

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string LastName { get; set; } = null!;

        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string UserName { get; set; } = null!;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = null!;

        [Required]
        [Phone]
        [StringLength(20)]
        public string PhoneNo { get; set; } = null!;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = null!;
    }
}