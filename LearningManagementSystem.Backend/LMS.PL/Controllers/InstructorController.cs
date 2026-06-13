using LMS.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.RateLimiting;

namespace LMS.PL.Controllers
{
    [ApiController]
    [Route("api/instructor")]
    [Authorize(Roles = "Instructor")]
    [EnableRateLimiting("api-limiter")]
    public class InstructorController : ControllerBase
    {
        private readonly IInstructorService _instructorService;

        public InstructorController(IInstructorService instructorService)
        {
            _instructorService = instructorService;
        }

        protected Guid CurrentUserGuid
        {
            get
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return Guid.TryParse(idClaim, out var guid) ? guid : Guid.Empty;
            }
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var stats = await _instructorService.GetDashboardDataAsync(CurrentUserGuid);
            return Ok(stats);
        }

        [HttpGet("courses")]
        public async Task<IActionResult> GetCourses()
        {
            var courses = await _instructorService.GetCoursesAsync(CurrentUserGuid);
            return Ok(courses);
        }

        [HttpGet("discussions")]
        public async Task<IActionResult> GetDiscussions([FromQuery] bool? unansweredOnly = null)
        {
            var discussions = await _instructorService.GetInstructorDiscussionsAsync(CurrentUserGuid, unansweredOnly);
            return Ok(discussions);
        }
    }
}
