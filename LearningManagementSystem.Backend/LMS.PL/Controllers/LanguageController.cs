using LMS.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace LMS.PL.Controllers
{
    [ApiController]
    [EnableRateLimiting("api-limiter")]
    public class LanguageController : ControllerBase
    {
        private readonly ILanguageService _languageService;

        public LanguageController(ILanguageService languageService)
        {
            _languageService = languageService;
        }

        [HttpGet("api/languages")]
        public async Task<IActionResult> GetApprovedLanguages()
        {
            var languages = await _languageService.GetLanguagesAsync(onlyApproved: true);
            return Ok(languages);
        }

        [HttpPost("api/languages")]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<IActionResult> CreateLanguage([FromBody] CreateLanguageRequest request)
        {
            bool isApproved = User.IsInRole("Admin");
            var result = await _languageService.CreateLanguageAsync(request.Name, isApproved);
            
            if (isApproved)
            {
                return Ok(new { language = result, message = "Language created and approved successfully" });
            }
            else
            {
                return Ok(new { language = result, message = "Language request submitted for Admin approval" });
            }
        }
    }

    public class CreateLanguageRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Language name must be between 2 and 50 characters.")]
        public string Name { get; set; } = null!;
    }
}
