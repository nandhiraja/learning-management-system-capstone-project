using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LMS.PL.Controllers
{
    [ApiController]
    [Route("api")]
    public class LectureController : ControllerBase
    {
        private readonly ILectureService _lectureService;

        public LectureController(ILectureService lectureService)
        {
            _lectureService = lectureService;
        }

        protected System.Guid CurrentUserGuid
        {
            get
            {
                var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                return System.Guid.TryParse(idClaim, out var guid) ? guid : System.Guid.Empty;
            }
        }

        [Authorize]
        [HttpGet("lectures/{lectureId}")]
        public async Task<IActionResult> GetLectureById(int lectureId)
        {
            var lecture = await _lectureService.GetLectureByIdAsync(lectureId, CurrentUserGuid);
            if (lecture == null) return NotFound();
            return Ok(lecture);
        }

        [Authorize(Roles = "Instructor,Admin")]
        [HttpPost("sections/{sectionId}/lectures")]
        public async Task<IActionResult> CreateLecture(int sectionId, [FromBody] LectureRequest request)
        {
            var response = await _lectureService.CreateLectureAsync(sectionId, request);
            return Ok(response);
        }

        [Authorize(Roles = "Instructor,Admin")]
        [HttpPut("lectures/{lectureId}")]
        public async Task<IActionResult> UpdateLecture(int lectureId, [FromBody] LectureRequest request)
        {
            var success = await _lectureService.UpdateLectureAsync(lectureId, request);
            if (!success) return NotFound();
            return Ok(new { message = "Updated" });
        }

        [Authorize(Roles = "Instructor,Admin")]
        [HttpDelete("lectures/{lectureId}")]
        public async Task<IActionResult> DeleteLecture(int lectureId)
        {
            var success = await _lectureService.DeleteLectureAsync(lectureId);
            if (!success) return NotFound();
            return Ok(new { message = "Deleted" });
        }
    }
}
