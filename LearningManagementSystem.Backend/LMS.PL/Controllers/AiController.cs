using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LMS.BLL.Interfaces;
using LMS.Core.Models;
using LMS.Core.Enums;
using LMS.DAL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Collections.Generic;

namespace LMS.PL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireAuth")]
    [EnableRateLimiting("api-limiter")]
    public class AiController : ControllerBase
    {
        private readonly ILectureRepository _lectureRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAiServiceClient _aiServiceClient;

        public AiController(
            ILectureRepository lectureRepository,
            IUserRepository userRepository,
            IAiServiceClient aiServiceClient)
        {
            _lectureRepository = lectureRepository;
            _userRepository = userRepository;
            _aiServiceClient = aiServiceClient;
        }

        protected Guid CurrentUserGuid
        {
            get
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return Guid.TryParse(idClaim, out var guid) ? guid : Guid.Empty;
            }
        }

        [HttpPost("ask")]
        public async Task<IActionResult> AskStudentQuestion([FromBody] AiAskRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Question))
                return BadRequest(new { message = "Question must not be empty." });

            var user = await _userRepository.Get(CurrentUserGuid);
            if (user == null) return Unauthorized(new { message = "User not found." });

            var lecture = await _lectureRepository.GetLectureWithDetailsAsync(request.LectureId);
            if (lecture == null) return NotFound(new { message = "Lecture not found." });

            // Check permissions using helper method
            var perms = EvaluatePermissions(user, lecture);
            if (!perms.IsAdmin && !perms.IsInstructor && !perms.IsEnrolled)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have permission to ask questions about this lecture." });
            }

            // Optimization: Get combined text directly instead of pulling full entities into memory
            var contextString = await _lectureRepository.GetCombinedTranscriptTextAsync(request.LectureId);
            if (string.IsNullOrWhiteSpace(contextString))
            {
                return NotFound(new { message = "No transcript available for this lecture yet." });
            }

            var aiResponse = await _aiServiceClient.AskQuestionAsync(contextString, request.Question, "student");
            if (!aiResponse.Success) return HandleAiServiceError(aiResponse);

            return Ok(new { answer = aiResponse.Data?.Answer, contextTruncated = aiResponse.Data?.ContextTruncated ?? false });
        }

        [HttpPost("instructor/ask")]
        [Authorize(Policy = "InstructorAccess")]
        public async Task<IActionResult> AskInstructorQuestion([FromBody] AiAskRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Question))
                return BadRequest(new { message = "Question must not be empty." });

            var user = await _userRepository.Get(CurrentUserGuid);
            if (user == null) return Unauthorized(new { message = "User not found." });

            var lecture = await _lectureRepository.GetLectureWithDetailsAsync(request.LectureId);
            if (lecture == null) return NotFound(new { message = "Lecture not found." });

            var perms = EvaluatePermissions(user, lecture);
            if (!perms.IsAdmin && !perms.IsInstructor)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only the course instructor or an admin can access instructor insights." });
            }

            var contextString = await _lectureRepository.GetCombinedTranscriptTextAsync(request.LectureId);
            if (string.IsNullOrWhiteSpace(contextString))
            {
                return NotFound(new { message = "No transcript available for this lecture yet." });
            }

            var aiResponse = await _aiServiceClient.AskQuestionAsync(contextString, request.Question, "instructor");
            if (!aiResponse.Success) return HandleAiServiceError(aiResponse);

            return Ok(new { answer = aiResponse.Data?.Answer, contextTruncated = aiResponse.Data?.ContextTruncated ?? false });
        }

        [HttpGet("lectures/{lectureId}/has-transcript")]
        public async Task<IActionResult> CheckTranscriptAvailability(int lectureId)
        {
            var user = await _userRepository.Get(CurrentUserGuid);
            if (user == null) return Unauthorized(new { message = "User not found." });

            var lecture = await _lectureRepository.GetLectureWithDetailsAsync(lectureId);
            if (lecture == null) return NotFound(new { message = "Lecture not found." });

            var perms = EvaluatePermissions(user, lecture);
            if (!perms.IsAdmin && !perms.IsInstructor && !perms.IsEnrolled)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have permission to view details about this lecture." });
            }

            var hasTranscript = await _lectureRepository.HasTranscriptAsync(lectureId);
            return Ok(new { hasTranscript });
        }

        // Centralized permission evaluator to prevent DRY violations
        private (bool IsAdmin, bool IsInstructor, bool IsEnrolled) EvaluatePermissions(User user, Lecture lecture)
        {
            bool isAdmin = user.Role?.Name?.Equals("Admin", StringComparison.OrdinalIgnoreCase) ?? false;
            bool isInstructor = lecture.CourseSection?.Course?.InstructorId == user.Id;
            bool isEnrolled = lecture.CourseSection?.Course?.Enrollments?.Any(e => e.UserId == user.Id && 
                                  (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed)) ?? false;

            return (isAdmin, isInstructor, isEnrolled);
        }

        private IActionResult HandleAiServiceError<T>(AiServiceEnvelope<T> response)
        {
            var errCode = response.Error?.Code;
            var errMessage = response.Error?.Message ?? "An error occurred with the AI service.";

            return errCode switch
            {
                "LLM_RATE_LIMITED" => StatusCode(StatusCodes.Status429TooManyRequests, new { message = errMessage }),
                "LLM_UNAVAILABLE" => StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = errMessage }),
                "CONTEXT_TOO_LONG" or "EMPTY_QUESTION" or "EMPTY_CONTEXT" => BadRequest(new { message = errMessage }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = errMessage }),
            };
        }
    }

    public class AiAskRequest
    {
        public int LectureId { get; set; }
        public string Question { get; set; } = null!;
    }
}
