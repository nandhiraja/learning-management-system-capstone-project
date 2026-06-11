using LMS.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.RateLimiting;

namespace LMS.PL.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("api-limiter")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ICourseService _courseService;
        private readonly IUserService _userService;

        public AdminController(IAdminService adminService, ICourseService courseService, IUserService userService)
        {
            _adminService = adminService;
            _courseService = courseService;
            _userService = userService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var data = await _adminService.GetDashboardDataAsync();
            return Ok(data);
        }

        [HttpGet("dashboard/pending")]
        public async Task<IActionResult> GetPendingQueue()
        {
            var pendingCourses = await _adminService.GetPendingCoursesAsync();
            var pendingInstructors = await _userService.GetPendingInstructorsAsync();
            return Ok(new
            {
                pendingCourses,
                pendingInstructors
            });
        }

        [HttpPost("courses/{courseId}/review")]
        public async Task<IActionResult> ReviewCourse(System.Guid courseId, [FromBody] CourseAdminReviewRequest request)
        {
            bool success;
            if (request.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
            {
                success = await _courseService.PublishCourseAsync(courseId);
            }
            else if (request.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            {
                success = await _courseService.RejectCourseAsync(courseId, request.Reason ?? "Rejected by Admin");
            }
            else
            {
                return BadRequest(new { message = "Invalid status. Use 'Approved' or 'Rejected'." });
            }

            if (!success) return BadRequest(new { message = "Failed to process course review status change" });
            return Ok(new { message = $"Course review processed: {request.Status}" });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userService.GetUsersAsync();
            return Ok(users);
        }

        [HttpPatch("users/{userGuid}/status")]
        public async Task<IActionResult> UpdateUserStatus(System.Guid userGuid, [FromBody] UserStatusUpdateRequest request)
        {
            bool success = request.IsActive 
                ? await _userService.UnblockUserAsync(userGuid) 
                : await _userService.BlockUserAsync(userGuid);

            if (!success) return BadRequest(new { message = "Failed to update user status" });
            return Ok(new { message = $"User status updated successfully to IsActive: {request.IsActive}" });
        }

        [HttpPut("users/{userGuid}/role")]
        public async Task<IActionResult> UpdateUserRole(System.Guid userGuid, [FromBody] UserRoleProgressionRequest request)
        {
            bool success;
            if (request.Action.Equals("ApproveInstructor", StringComparison.OrdinalIgnoreCase))
            {
                success = await _userService.ApproveInstructorAsync(userGuid);
            }
            else if (request.Action.Equals("RejectInstructor", StringComparison.OrdinalIgnoreCase))
            {
                success = await _userService.RejectInstructorAsync(userGuid);
            }
            else if (request.Action.Equals("DemoteToStudent", StringComparison.OrdinalIgnoreCase))
            {
                success = await _userService.DemoteToStudentAsync(userGuid);
            }
            else
            {
                return BadRequest(new { message = "Invalid action. Use 'ApproveInstructor', 'RejectInstructor', or 'DemoteToStudent'." });
            }

            if (!success) return BadRequest(new { message = "Failed to update user role progression" });
            return Ok(new { message = "User role updated successfully" });
        }
    }

    public class CourseAdminReviewRequest
    {
        [Required]
        public string Status { get; set; } = null!; // "Approved" or "Rejected"
        public string? Reason { get; set; }
    }

    public class UserStatusUpdateRequest
    {
        [Required]
        public bool IsActive { get; set; }
    }

    public class UserRoleProgressionRequest
    {
        [Required]
        public string Action { get; set; } = null!; // "ApproveInstructor", "RejectInstructor", "DemoteToStudent"
    }
}
