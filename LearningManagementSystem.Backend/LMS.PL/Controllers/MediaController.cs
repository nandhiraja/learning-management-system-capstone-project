using Microsoft.AspNetCore.Mvc;
using LMS.BLL.Interfaces;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.StaticFiles;

namespace LMS.PL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // Notice we do NOT use [Authorize] here because standard HTML video/object tags 
    // do not support sending Authorization headers. We rely on the secure token in the query string.
    public class MediaController : ControllerBase
    {
        private readonly IMediaTokenService _mediaTokenService;
        private readonly IFileStorageService _fileStorageService;

        public MediaController(IMediaTokenService mediaTokenService, IFileStorageService fileStorageService)
        {
            _mediaTokenService = mediaTokenService;
            _fileStorageService = fileStorageService;
        }

        protected Guid CurrentUserGuid
        {
            get
            {
                // Note: Since this endpoint is hit anonymously by the browser, User.Identity might be null.
                // The actual user identity is embedded and verified inside the token!
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return Guid.TryParse(idClaim, out var guid) ? guid : Guid.Empty;
            }
        }

        [HttpGet("authorize")]
        public IActionResult Authorize([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized("Token is required.");
            }

            if (!_mediaTokenService.ValidateToken(token, out var _))
            {
                return Unauthorized("Invalid or expired media token.");
            }

            var isHttps = Request.IsHttps;
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isHttps,
                SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            Response.Cookies.Append("LMS_MediaAuth", token, cookieOptions);
            return Ok();
        }

        [HttpGet("stream/{*filePath}")]
        public async Task<IActionResult> StreamMedia(string filePath)
        {
            filePath = System.Uri.UnescapeDataString(filePath);

            if (!Request.Cookies.TryGetValue("LMS_MediaAuth", out var token) || string.IsNullOrEmpty(token))
            {
                return Unauthorized("Authentication cookie is missing.");
            }

            if (!_mediaTokenService.ValidateToken(token, out var authorizedPath))
            {
                return Unauthorized("Invalid or expired media token.");
            }

            // Verify that the requested file path belongs to the authorized directory
            // E.g., authorizedPath might be "secure_uploads/videos/123/playlist.m3u8"
            // The requested path might be "secure_uploads/videos/123/segment_000.ts"
            // As long as they share the same base directory, we allow it.
            var authorizedDir = Path.GetDirectoryName(authorizedPath)?.Replace("\\", "/");
            var requestedDir = Path.GetDirectoryName(filePath)?.Replace("\\", "/");

            if (string.IsNullOrEmpty(authorizedDir) || string.IsNullOrEmpty(requestedDir) || !requestedDir.StartsWith(authorizedDir, StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, "You are not authorized to access this file.");
            }

            var (stream, contentType) = await _fileStorageService.GetFileStreamAsync(filePath);
            if (stream == null)
            {
                return NotFound("File not found.");
            }

            return File(stream, contentType, enableRangeProcessing: true);
        }
    }
}
