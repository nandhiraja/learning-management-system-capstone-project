using LMS.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LMS.PL.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ICourseService _courseService;

        public AdminController(IAdminService adminService, ICourseService courseService)
        {
            _adminService = adminService;
            _courseService = courseService;
        }

        [HttpGet("dashboard/pending-courses")]
        public async Task<IActionResult> GetPendingCourses()
        {
            var courses = await _adminService.GetPendingCoursesAsync();
            return Ok(courses);
        }

        [HttpPost("courses/{courseId}/approve")]
        public async Task<IActionResult> ApproveCourse(System.Guid courseId)
        {
            var success = await _courseService.PublishCourseAsync(courseId);
            if (!success) return BadRequest(new { message = "Failed to approve course" });
            return Ok(new { message = "Course approved successfully" });
        }

        [HttpPost("courses/{courseId}/reject")]
        public async Task<IActionResult> RejectCourse(System.Guid courseId, [FromBody] RejectCourseRequest request)
        {
            var success = await _courseService.RejectCourseAsync(courseId, request.Reason);
            if (!success) return BadRequest(new { message = "Failed to reject course" });
            return Ok(new { message = "Course rejected successfully" });
        }
    }

    public class RejectCourseRequest
    {
        public string Reason { get; set; } = null!;
    }
}
