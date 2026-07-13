using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.RateLimiting;

namespace LMS.PL.Controllers
{
    [ApiController]
    [Route("api/users")]
    [EnableRateLimiting("api-limiter")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        protected Guid CurrentUserGuid
        {
            get
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return Guid.TryParse(idClaim, out var guid) ? guid : Guid.Empty;
            }
        }

        [Authorize(Policy = "RequireAuth")]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var profile = await _userService.GetProfileAsync(CurrentUserGuid);
            return Ok(profile);
        }

        [Authorize(Policy = "RequireAuth")]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UserEditRequest request)
        {
            var success = await _userService.UpdateProfileAsync(CurrentUserGuid, request);
            if (!success) return BadRequest(new { message = "Update failed" });
            return Ok(new { message = "Profile updated" });
        }

        [Authorize(Policy = "RequireAuth")]
        [HttpPost("become-instructor")]
        public async Task<IActionResult> BecomeInstructor()
        {
            var success = await _userService.BecomeInstructorAsync(CurrentUserGuid);
            if (!success) return BadRequest(new { message = "Failed to submit request" });
            return Ok(new { message = "Instructor request submitted successfully. Waiting for admin approval." });
        }

        [Authorize(Policy = "RequireAuth")]
        [HttpPost("me/certificate-name")]
        public async Task<IActionResult> UpdateCertificateName([FromBody] CertificateNameUpdateRequest request)
        {
            try
            {
                var success = await _userService.UpdateCertificateNameAsync(CurrentUserGuid, request.NewName);
                if (!success) return BadRequest(new { message = "Failed to update certificate name" });
                return Ok(new { message = "Certificate name updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class CertificateNameUpdateRequest
    {
        public string NewName { get; set; } = null!;
    }
}
