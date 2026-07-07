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
        private readonly ICategoryService _categoryService;
        private readonly ILanguageService _languageService;
        private readonly ICertificateService _certificateService;

        public AdminController(
            IAdminService adminService, 
            ICourseService courseService, 
            IUserService userService, 
            ICategoryService categoryService, 
            ILanguageService languageService,
            ICertificateService certificateService)
        {
            _adminService = adminService;
            _courseService = courseService;
            _userService = userService;
            _categoryService = categoryService;
            _languageService = languageService;
            _certificateService = certificateService;
        }

        [HttpPost("certificates/regenerate-all")]
        public async Task<IActionResult> RegenerateAllCertificates()
        {
            var count = await _certificateService.RegenerateAllCertificatesAsync();
            return Ok(new { message = $"Successfully regenerated {count} certificates." });
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

        [HttpPost("courses/{courseId}/archive")]
        public async Task<IActionResult> ArchiveCourse(System.Guid courseId, [FromBody] CourseAdminArchiveRequest request)
        {
            try
            {
                // CurrentUserGuid is not declared in AdminController, let's parse Guid from ClaimTypes.NameIdentifier
                var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var currentAdminGuid = Guid.TryParse(idClaim, out var guid) ? guid : Guid.Empty;

                var success = await _courseService.ArchiveCourseAsync(courseId, currentAdminGuid, isAdmin: true, request.Reason);
                if (!success) return BadRequest("Could not archive the course. Ensure it is currently published.");
                return Ok(new { message = "Course archived successfully by Admin" });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userService.GetUsersAsync();
            return Ok(users);
        }

        [HttpGet("courses")]
        public async Task<IActionResult> GetCourses()
        {
            var courses = await _adminService.GetAdminCoursesAsync();
            return Ok(courses);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetAdminCategories()
        {
            var categories = await _categoryService.GetCategoriesAsync(onlyApproved: false);
            return Ok(categories);
        }

        [HttpPut("categories/{id}/approve")]
        public async Task<IActionResult> ApproveCategory(int id)
        {
            var success = await _categoryService.ApproveCategoryAsync(id);
            if (!success) return NotFound(new { message = "Category not found" });
            return Ok(new { message = "Category approved successfully" });
        }

        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var success = await _categoryService.DeleteCategoryAsync(id);
            if (!success) return NotFound(new { message = "Category not found" });
            return Ok(new { message = "Category deleted successfully" });
        }

        [HttpGet("languages")]
        public async Task<IActionResult> GetAdminLanguages()
        {
            var languages = await _languageService.GetLanguagesAsync(onlyApproved: false);
            return Ok(languages);
        }

        [HttpPut("languages/{id}/approve")]
        public async Task<IActionResult> ApproveLanguage(int id)
        {
            var success = await _languageService.ApproveLanguageAsync(id);
            if (!success) return NotFound(new { message = "Language not found" });
            return Ok(new { message = "Language approved successfully" });
        }

        [HttpDelete("languages/{id}")]
        public async Task<IActionResult> DeleteLanguage(int id)
        {
            var success = await _languageService.DeleteLanguageAsync(id);
            if (!success) return NotFound(new { message = "Language not found" });
            return Ok(new { message = "Language deleted successfully" });
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

    public class CourseAdminArchiveRequest
    {
        [Required]
        public string Reason { get; set; } = null!;
    }
}
