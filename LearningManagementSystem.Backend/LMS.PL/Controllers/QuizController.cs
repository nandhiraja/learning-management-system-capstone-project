using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.RateLimiting;

namespace LMS.PL.Controllers
{
    [ApiController]
    [Route("api")]
    [EnableRateLimiting("api-limiter")]
    public class QuizController : ControllerBase
    {
        private readonly IQuizService _quizService;

        public QuizController(IQuizService quizService)
        {
            _quizService = quizService;
        }

        protected Guid CurrentUserGuid
        {
            get
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return Guid.TryParse(idClaim, out var guid) ? guid : Guid.Empty;
            }
        }

        [HttpGet("quizzes/{quizId}")]
        public async Task<IActionResult> GetQuizById(int quizId)
        {
            var quiz = await _quizService.GetQuizByIdAsync(quizId);
            if (quiz == null) return NotFound();
            return Ok(quiz);
        }

        [Authorize(Policy = "InstructorAccess")]
        [HttpPost("lectures/{lectureId}/quiz")]
        public async Task<IActionResult> CreateQuiz(int lectureId, [FromBody] QuizRequest request)
        {
            var response = await _quizService.CreateQuizAsync(lectureId, request, CurrentUserGuid);
            return Ok(response);
        }

        [Authorize(Policy = "InstructorAccess")]
        [HttpPut("quizzes/{quizId}")]
        public async Task<IActionResult> UpdateQuiz(int quizId, [FromBody] QuizRequest request)
        {
            var success = await _quizService.UpdateQuizAsync(quizId, request, CurrentUserGuid);
            if (!success) return NotFound();
            return Ok(new { message = "Updated" });
        }

        [Authorize(Policy = "InstructorAccess")]
        [HttpDelete("quizzes/{quizId}")]
        public async Task<IActionResult> DeleteQuiz(int quizId)
        {
            var success = await _quizService.DeleteQuizAsync(quizId, CurrentUserGuid);
            if (!success) return NotFound();
            return Ok(new { message = "Deleted" });
        }

        [Authorize(Policy = "StudentAccess")]
        [HttpPost("quizzes/{quizId}/submit")]
        public async Task<IActionResult> SubmitQuiz(int quizId, [FromBody] QuizSubmitRequest request)
        {
            var response = await _quizService.SubmitQuizAnswersAsync(quizId, CurrentUserGuid, request);
            return Ok(response);
        }

        [Authorize(Policy = "StudentAccess")]
        [HttpGet("quizzes/{quizId}/progress")]
        public async Task<IActionResult> GetQuizProgress(int quizId)
        {
            var response = await _quizService.GetQuizProgressAsync(quizId, CurrentUserGuid);
            return Ok(response);
        }
    }
}
