using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LMS.PL.Controllers
{
    [ApiController]
    [Authorize]
    [EnableRateLimiting("api-limiter")]
    public class DiscussionController : ControllerBase
    {
        private readonly IDiscussionService _discussionService;

        public DiscussionController(IDiscussionService discussionService)
        {
            _discussionService = discussionService;
        }

        protected Guid CurrentUserGuid
        {
            get
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return Guid.TryParse(idClaim, out var guid) ? guid : Guid.Empty;
            }
        }

        [HttpPost("api/courses/{courseId}/discussions")]
        public async Task<IActionResult> CreateDiscussion(Guid courseId, [FromBody] DiscussionCreateRequest request)
        {
            var result = await _discussionService.CreateDiscussionAsync(courseId, CurrentUserGuid, request);
            return CreatedAtAction(nameof(GetDiscussionDetails), new { discussionId = result.ExternalId }, result);
        }

        [HttpGet("api/courses/{courseId}/discussions")]
        public async Task<IActionResult> GetDiscussions(Guid courseId)
        {
            var result = await _discussionService.GetDiscussionsForCourseAsync(courseId, CurrentUserGuid);
            return Ok(result);
        }

        [HttpGet("api/discussions/{discussionId}")]
        public async Task<IActionResult> GetDiscussionDetails(Guid discussionId)
        {
            var result = await _discussionService.GetDiscussionDetailsAsync(discussionId, CurrentUserGuid);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("api/discussions/{discussionId}/replies")]
        public async Task<IActionResult> CreateReply(Guid discussionId, [FromBody] DiscussionReplyCreateRequest request)
        {
            var result = await _discussionService.CreateReplyAsync(discussionId, CurrentUserGuid, request);
            return Ok(result);
        }
    }
}
