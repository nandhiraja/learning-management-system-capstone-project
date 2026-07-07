using LMS.BLL.Interfaces;
using LMS.Core.DTOs.PublicDTOs;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.PL.Controllers
{
    [ApiController]
    [Route("api/public")]
    public class PublicController : ControllerBase
    {
        private readonly IPublicService _publicService;

        public PublicController(IPublicService publicService)
        {
            _publicService = publicService;
        }

        [HttpGet("landing-stats")]
        public async Task<ActionResult<LandingStatsResponse>> GetLandingStats()
        {
            var stats = await _publicService.GetLandingStatsAsync();
            return Ok(stats);
        }

        [HttpGet("top-instructors")]
        public async Task<ActionResult<IEnumerable<TopInstructorResponse>>> GetTopInstructors([FromQuery] int limit = 4)
        {
            var instructors = await _publicService.GetTopInstructorsAsync(limit);
            return Ok(instructors);
        }
    }
}
