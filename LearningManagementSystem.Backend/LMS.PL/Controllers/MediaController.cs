using Microsoft.AspNetCore.Mvc;
using LMS.BLL.Interfaces;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Text.RegularExpressions;

namespace LMS.PL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MediaController : ControllerBase
    {
        private readonly IStorageService _storageService;

        public MediaController(IStorageService storageService)
        {
            _storageService = storageService;
        }

        [HttpGet("stream")]
        public async Task<IActionResult> StreamVideo([FromQuery] string path)
        {
            try
            {
                // We extract the relative path from the full URL if necessary
                var relativePath = ExtractRelativePath(path);
                var stream = await _storageService.GetFileAsync(relativePath);
                return File(stream, "video/mp4", enableRangeProcessing: true);
            }
            catch (FileNotFoundException)
            {
                return NotFound("Video file not found.");
            }
        }

        [HttpGet("document")]
        public async Task<IActionResult> GetDocument([FromQuery] string path)
        {
            try
            {
                var relativePath = ExtractRelativePath(path);
                var stream = await _storageService.GetFileAsync(relativePath);
                
                string contentType = "application/pdf";
                if (relativePath.EndsWith(".txt") || relativePath.EndsWith(".md"))
                {
                    contentType = "text/plain";
                }

                return File(stream, contentType);
            }
            catch (FileNotFoundException)
            {
                return NotFound("Document file not found.");
            }
        }

        private string ExtractRelativePath(string fullUrlOrPath)
        {
            if (string.IsNullOrEmpty(fullUrlOrPath)) return string.Empty;

            // If it's a full URL (e.g. http://localhost:5159/files/videos/xyz.mp4)
            // we just want to extract the path portion starting from "files/"
            if (fullUrlOrPath.Contains("/files/"))
            {
                var index = fullUrlOrPath.IndexOf("/files/");
                // The storage service prepends wwwroot, and the static files maps "files" to "wwwroot/uploads".
                // Since LocalStorageService currently assumes it's reading relative to wwwroot,
                // we should map "/files/xyz" to "uploads/xyz".
                var subPath = fullUrlOrPath.Substring(index + 7); // Skip "/files/"
                return "uploads/" + subPath;
            }

            // Fallback
            return fullUrlOrPath.TrimStart('/');
        }
    }
}
