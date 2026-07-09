using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
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
        private readonly ICertificateService _certificateService;

        public PublicController(IPublicService publicService, ICertificateService certificateService)
        {
            _publicService = publicService;
            _certificateService = certificateService;
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

        [HttpGet("certificates/verify/{verificationId}")]
        public async Task<ActionResult<CertificateResponse>> VerifyCertificate(string verificationId)
        {
            var certificate = await _certificateService.GetCertificateByVerificationIdAsync(verificationId);
            if (certificate == null)
            {
                return NotFound(new { message = "Certificate not found or invalid." });
            }
            return Ok(certificate);
        }
    }
}
