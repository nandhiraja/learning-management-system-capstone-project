using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.RateLimiting;

namespace LMS.PL.Controllers
{
    [ApiController]
    [Route("api")]
    [EnableRateLimiting("api-limiter")]
    public class CourseSectionController : ControllerBase
    {
        private readonly ICourseSectionService _sectionService;

        public CourseSectionController(ICourseSectionService sectionService)
        {
            _sectionService = sectionService;
        }

        protected Guid CurrentUserGuid
        {
            get
            {
                var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                return Guid.TryParse(idClaim, out var guid) ? guid : Guid.Empty;
            }
        }

        [Authorize(Policy = "InstructorAccess")]
        [HttpPost("courses/{courseId}/sections")]
        public async Task<IActionResult> CreateSection(Guid courseId, [FromBody] CourseSectionRequest request)
        {
            var response = await _sectionService.CreateSectionAsync(courseId, request, CurrentUserGuid);
            return Ok(response);
        }

        [Authorize(Policy = "InstructorAccess")]
        [HttpPut("sections/{sectionId}")]
        public async Task<IActionResult> UpdateSection(int sectionId, [FromBody] CourseSectionRequest request)
        {
            var success = await _sectionService.UpdateSectionAsync(sectionId, request, CurrentUserGuid);
            if (!success) return NotFound();
            return Ok(new { message = "Updated" });
        }

        [Authorize(Policy = "InstructorAccess")]
        [HttpDelete("sections/{sectionId}")]
        public async Task<IActionResult> DeleteSection(int sectionId)
        {
            var success = await _sectionService.DeleteSectionAsync(sectionId, CurrentUserGuid);
            if (!success) return NotFound();
            return Ok(new { message = "Deleted" });
        }
    }
}
