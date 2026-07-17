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

using LMS.DAL.Data;
using LMS.Core.DTOs;
using Microsoft.EntityFrameworkCore;

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
        private readonly LMSDBContext _context;

        public AiController(
            ILectureRepository lectureRepository,
            IUserRepository userRepository,
            IAiServiceClient aiServiceClient,
            LMSDBContext context)
        {
            _lectureRepository = lectureRepository;
            _userRepository = userRepository;
            _aiServiceClient = aiServiceClient;
            _context = context;
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

        [HttpPost("quizzes/{quizId}/study-plan")]
        [Authorize(Policy = "StudentAccess")]
        public async Task<IActionResult> GenerateStudyPlan(int quizId, [FromBody] QuizSubmitRequest submitRequest)
        {
            if (submitRequest == null || submitRequest.Answers == null || !submitRequest.Answers.Any())
            {
                return BadRequest(new { message = "Submitted answers cannot be empty." });
            }

            var user = await _userRepository.Get(CurrentUserGuid);
            if (user == null) return Unauthorized(new { message = "User not found." });

            // 1. Fetch Quiz with questions and correct options
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Options)
                .Include(q => q.Course)
                    .ThenInclude(c => c.Sections)
                        .ThenInclude(cs => cs.Lectures)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null) return NotFound(new { message = "Quiz not found." });

            // 2. Identify incorrect questions
            var wrongQuestions = new List<StudyPlanWrongQuestionDto>();
            foreach (var answer in submitRequest.Answers)
            {
                var question = quiz.Questions.FirstOrDefault(q => q.Id == answer.QuestionId);
                if (question != null)
                {
                    var selectedOption = question.Options.FirstOrDefault(o => o.Id == answer.OptionId);
                    var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);

                    if (selectedOption == null || !selectedOption.IsCorrect)
                    {
                        wrongQuestions.Add(new StudyPlanWrongQuestionDto
                        {
                            QuestionText = question.QuestionText,
                            StudentAnswerText = selectedOption?.OptionText ?? "(No answer selected or invalid option)",
                            CorrectAnswerText = correctOption?.OptionText ?? "(Correct answer not configured)"
                        });
                    }
                }
            }

            // If they got everything correct, no remediation is needed!
            if (!wrongQuestions.Any())
            {
                return Ok(new { studyPlan = "Congratulations! You got a perfect score. No study plan needed!" });
            }

            // 3. Find the course section that this quiz belongs to
            var courseOutline = quiz.Course.Sections
                .OrderBy(cs => cs.Order)
                .Select(cs => $"{cs.Title}: {string.Join(", ", cs.Lectures.OrderBy(l => l.Id).Select(l => l.Title))}")
                .ToList();

            // 4. Retrieve transcripts for lectures in this course
            var lectureIds = quiz.Course.Sections
                .SelectMany(cs => cs.Lectures)
                .Select(l => l.Id)
                .ToList();

            var transcripts = await _context.LectureTranscripts
                .Where(t => lectureIds.Contains(t.LectureId))
                .OrderBy(t => t.LectureId)
                .ThenBy(t => t.StartTime)
                .Select(t => new { t.Lecture.Title, t.StartTime, t.Text })
                .ToListAsync();

            // Format transcripts for the AI, handling fallback when transcripts are missing
            string contextTranscripts = "No lecture transcripts are available for this section.";
            if (transcripts.Any())
            {
                var formatted = transcripts.Select(t =>
                {
                    var time = TimeSpan.FromSeconds(t.StartTime);
                    string timestamp = time.TotalHours >= 1 ? time.ToString(@"hh\:mm\:ss") : time.ToString(@"mm\:ss");
                    return $"[Lecture: {t.Title}] [{timestamp}] {t.Text}";
                });
                contextTranscripts = string.Join(" ", formatted);
            }

            // Fetch QuizProgress early to get attempts used
            var quizProgress = await _context.QuizProgresses.FirstOrDefaultAsync(qp => qp.UserId == user.Id && qp.QuizId == quizId);

            // 5. Call Python AI microservice
            var aiRequest = new StudyPlanRequestDto
            {
                QuizTitle = quiz.Title,
                WrongQuestions = wrongQuestions,
                CourseOutline = courseOutline,
                ContextTranscripts = contextTranscripts,
                AttemptsUsed = quizProgress?.AttemptsUsed ?? 1,
                MaxAttempts = quiz.MaxAttempts
            };

            var aiResponse = await _aiServiceClient.GenerateStudyPlanAsync(aiRequest);
            if (!aiResponse.Success) return HandleAiServiceError(aiResponse);

            var studyPlanMarkdown = aiResponse.Data?.StudyPlanMarkdown ?? "Unable to generate study plan at this moment.";

            // 6. Persist to QuizProgress
            if (quizProgress != null)
            {
                quizProgress.LastStudyPlan = studyPlanMarkdown;
                await _context.SaveChangesAsync();
            }

            return Ok(new { studyPlan = studyPlanMarkdown });
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

        [HttpPost("lectures/{lectureId}/generate-quiz")]
        [Authorize(Policy = "InstructorAccess")]
        public async Task<IActionResult> GenerateQuizFromLecture(int lectureId, [FromBody] GenerateQuizRequestDto? request)
        {
            var user = await _userRepository.Get(CurrentUserGuid);
            if (user == null) return Unauthorized(new { message = "User not found." });

            var lecture = await _lectureRepository.GetLectureWithDetailsAsync(lectureId);
            if (lecture == null) return NotFound(new { message = "Lecture not found." });

            var perms = EvaluatePermissions(user, lecture);
            if (!perms.IsAdmin && !perms.IsInstructor)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only instructors or admins can generate quizzes." });
            }

            var req = request ?? new GenerateQuizRequestDto();

            // Fetch transcript segments for this lecture
            var transcripts = await _context.LectureTranscripts
                .Where(t => t.LectureId == lectureId)
                .OrderBy(t => t.StartTime)
                .Select(t => new { t.StartTime, t.Text })
                .ToListAsync();

            if (!transcripts.Any())
            {
                return NotFound(new { message = "No transcript available for this lecture. Please upload and process the lecture video first." });
            }

            // Format transcript as timestamped text for the AI agent
            var formatted = transcripts.Select(t =>
            {
                var time = TimeSpan.FromSeconds(t.StartTime);
                string ts = time.TotalHours >= 1 ? time.ToString(@"hh\:mm\:ss") : time.ToString(@"mm\:ss");
                return $"[{ts}] {t.Text}";
            });
            var fullTranscript = string.Join(" ", formatted);

            var aiResponse = await _aiServiceClient.GenerateQuizAsync(fullTranscript, lecture.Title, req.NumQuestions, req.ExistingQuestions);
            if (!aiResponse.Success) return HandleAiServiceError(aiResponse);

            return Ok(new { questions = aiResponse.Data });
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
