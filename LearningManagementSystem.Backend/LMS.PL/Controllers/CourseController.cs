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
    [Route("api/courses")]
    [EnableRateLimiting("api-limiter")]
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly ICourseReviewService _reviewService;

        public CourseController(ICourseService courseService, ICourseReviewService reviewService)
        {
            _courseService = courseService;
            _reviewService = reviewService;
        }

        protected Guid CurrentUserGuid
        {
            get
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return Guid.TryParse(idClaim, out var guid) ? guid : Guid.Empty;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCourses(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? search = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] string? language = null,
            [FromQuery] string? sortBy = null)
        {
            var result = await _courseService.GetCoursesAsync(page, pageSize, categoryId, search, minPrice, maxPrice, language, sortBy);
            return Ok(new { items = result.Items, totalCount = result.TotalCount });
        }

        [HttpGet("{courseId}")]
        public async Task<IActionResult> GetCourseById(Guid courseId)
        {
            var course = await _courseService.GetCourseByIdAsync(courseId, CurrentUserGuid);
            if (course == null) return NotFound();
            return Ok(course);
        }

        [Authorize(Roles = "Instructor,Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateCourse([FromBody] CourseCreateRequest request)
        {
            var course = await _courseService.CreateCourseAsync(request, CurrentUserGuid);
            return CreatedAtAction(nameof(GetCourseById), new { courseId = course.ExternalId }, course);
        }

        [Authorize(Roles = "Instructor,Admin")]
        [HttpPut("{courseId}")]
        public async Task<IActionResult> UpdateCourse(Guid courseId, [FromBody] CourseUpdateRequest request)
        {
            var result = await _courseService.UpdateCourseAsync(courseId, request, CurrentUserGuid);
            if (result == null || !result.Success) return NotFound();
            return Ok(new { message = "Course updated", updatedCourseGuid = result.UpdatedCourseGuid });
        }

        [Authorize(Roles = "Instructor,Admin")]
        [HttpDelete("{courseId}")]
        public async Task<IActionResult> DeleteCourse(Guid courseId)
        {
            var success = await _courseService.DeleteCourseAsync(courseId, CurrentUserGuid);
            if (!success) return NotFound();
            return Ok(new { message = "Course deleted" });
        }

        [Authorize(Roles = "Instructor")]
        [HttpPost("{courseId}/submit")]
        public async Task<IActionResult> SubmitForReview(Guid courseId)
        {
            var success = await _courseService.SubmitForReviewAsync(courseId, CurrentUserGuid);
            if (!success) return BadRequest("Could not submit for review");
            return Ok(new { message = "Submitted for admin approval" });
        }

        [Authorize]
        [HttpPost("{courseId}/reviews")]
        public async Task<IActionResult> AddReview(Guid courseId, [FromBody] ReviewRequest request)
        {
            var response = await _reviewService.AddReviewAsync(courseId, CurrentUserGuid, request);
            return Ok(response);
        }

        [HttpGet("{courseId}/reviews")]
        public async Task<IActionResult> GetReviews(Guid courseId)
        {
            var reviews = await _reviewService.GetReviewsByCourseAsync(courseId);
            return Ok(reviews);
        }

        [Authorize]
        [HttpDelete("~/api/reviews/{reviewId}")]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            var success = await _reviewService.DeleteReviewAsync(reviewId, CurrentUserGuid);
            if (!success) return NotFound();
            return Ok(new { message = "Review deleted" });
        }
    }
}
