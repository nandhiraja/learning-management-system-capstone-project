using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.RateLimiting;
using LMS.Core.Models;

namespace LMS.PL.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize(Roles = "Student")]
    [EnableRateLimiting("api-limiter")]
    public class EnrollmentController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;
        private readonly ILectureProgressService _progressService;
        private readonly ICertificateService _certificateService;

        public EnrollmentController(
            IEnrollmentService enrollmentService, 
            ILectureProgressService progressService, 
            ICertificateService certificateService)
        {
            _enrollmentService = enrollmentService;
            _progressService = progressService;
            _certificateService = certificateService;
        }

        protected Guid CurrentUserGuid
        {
            get
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return Guid.TryParse(idClaim, out var guid) ? guid : Guid.Empty;
            }
        }

        // --- Enrollments ---

        [HttpGet("enrollments")]
        public async Task<IActionResult> GetMyCourses()
        {
            var courses = await _enrollmentService.GetUserCoursesAsync(CurrentUserGuid);
            return Ok(courses);
        }

        // --- Progress ---

        [HttpGet("enrollments/{enrollmentId}/progress")]
        public async Task<IActionResult> GetCourseProgress(int enrollmentId)
        {
            var progress = await _progressService.GetProgressAsync(CurrentUserGuid, enrollmentId);
            return Ok(progress);
        }

        [HttpPut("enrollments/{enrollmentId}/lectures/{lectureId}/progress")]
        public async Task<IActionResult> UpdateProgress(int enrollmentId, int lectureId, [FromBody] ProgressRequestDto request)
        {
            var progressUpdate = new ProgressUpdateRequest
            {
                LectureId = lectureId,
                WatchedSeconds = request.WatchedSeconds,
                IsCompleted = request.IsCompleted
            };
            var success = await _progressService.UpdateProgressAsync(CurrentUserGuid, progressUpdate);
            if (!success) return BadRequest("Could not save progress");
            return Ok(new { message = "Saved" });
        }

        // --- Certificates ---

        [HttpGet("certificates")]
        public async Task<IActionResult> GetMyCertificates()
        {
            var certificates = await _certificateService.GetCertificatesByUserAsync(CurrentUserGuid);
            return Ok(certificates);
        }

        [HttpGet("certificates/{certificateId}")]
        public async Task<IActionResult> GetCertificate(int certificateId)
        {
            var certificate = await _certificateService.GetCertificateByIdAsync(certificateId);
            if (certificate == null) return NotFound("Certificate not found");
            return Ok(certificate);
        }
    }

}
