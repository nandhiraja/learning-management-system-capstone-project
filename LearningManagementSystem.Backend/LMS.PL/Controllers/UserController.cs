using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LMS.PL.Controllers
{
    [ApiController]
    [Route("api/users")]
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

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var profile = await _userService.GetProfileAsync(CurrentUserGuid);
            return Ok(profile);
        }

        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UserEditRequest request)
        {
            var success = await _userService.UpdateProfileAsync(CurrentUserGuid, request);
            if (!success) return BadRequest(new { message = "Update failed" });
            return Ok(new { message = "Profile updated" });
        }

        [Authorize]
        [HttpPost("become-instructor")]
        public async Task<IActionResult> BecomeInstructor()
        {
            var success = await _userService.BecomeInstructorAsync(CurrentUserGuid);
            if (!success) return BadRequest(new { message = "Failed to upgrade role" });
            return Ok(new { message = "You have successfully become an instructor." });
        }
    }
}
