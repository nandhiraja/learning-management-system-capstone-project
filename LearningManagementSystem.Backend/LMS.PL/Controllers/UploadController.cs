using LMS.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.RateLimiting;

namespace LMS.PL.Controllers
{
    [ApiController]
    [Route("api/upload")]
    [Authorize] // Require users to be logged in to upload files
    [EnableRateLimiting("upload-limiter")]
    public class UploadController : ControllerBase
    {
        private readonly IFileStorageService _fileStorageService;

        public UploadController(IFileStorageService fileStorageService)
        {
            _fileStorageService = fileStorageService;
        }

        [HttpPost("image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file was uploaded.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
                return BadRequest("Invalid image format. Allowed formats: JPG, JPEG, PNG, GIF, WEBP.");

            // 5MB limit for images
            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("Image size exceeds the 5MB limit.");

            try
            {
                var fileUrl = await _fileStorageService.SaveFileAsync(file, "images");
                return Ok(new { url = fileUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("pdf")]
        public async Task<IActionResult> UploadPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file was uploaded.");

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".pdf")
                return BadRequest("Invalid file format. Only PDF files are allowed.");

            // 20MB limit for PDFs
            if (file.Length > 20 * 1024 * 1024)
                return BadRequest("PDF size exceeds the 20MB limit.");

            try
            {
                var fileUrl = await _fileStorageService.SaveFileAsync(file, "pdfs");
                return Ok(new { url = fileUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("video")]
        [Authorize(Roles = "Instructor")] // Only instructors can upload course videos
        public async Task<IActionResult> UploadVideo(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file was uploaded.");

            var allowedExtensions = new[] { ".mp4", ".mov", ".avi", ".mkv", ".webm" };
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
                return BadRequest("Invalid video format. Allowed formats: MP4, MOV, AVI, MKV, WEBM.");

            // 200MB limit for course videos
            if (file.Length > 200 * 1024 * 1024)
                return BadRequest("Video size exceeds the 200MB limit.");

            try
            {
                var fileUrl = await _fileStorageService.SaveFileAsync(file, "videos");
                return Ok(new { url = fileUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("document")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file was uploaded.");

            var allowedExtensions = new[] { ".md", ".txt", ".ppt", ".pptx", ".doc", ".docx", ".xls", ".xlsx" };
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
                return BadRequest("Invalid document format. Allowed formats: MD, TXT, PPT, PPTX, DOC, DOCX, XLS, XLSX.");

            // 20MB limit for documents
            if (file.Length > 20 * 1024 * 1024)
                return BadRequest("Document size exceeds the 20MB limit.");

            try
            {
                var fileUrl = await _fileStorageService.SaveFileAsync(file, "documents");
                return Ok(new { url = fileUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
