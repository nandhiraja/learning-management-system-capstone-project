using LMS.BLL.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LMS.BLL.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public FileStorageService(IWebHostEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _configuration = configuration;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            var webRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var isSecure = folderName.ToLower() == "videos" || folderName.ToLower() == "documents" || folderName.ToLower() == "pdfs";
            
            var storagePath = isSecure 
                ? _configuration["FileStorage:SecureMediaStoragePath"] ?? "wwwroot/secure_uploads"
                : _configuration["FileStorage:PublicMediaStoragePath"] ?? "wwwroot/uploads";

            var absoluteStoragePath = Path.Combine(Directory.GetCurrentDirectory(), storagePath, folderName);

            if (!Directory.Exists(absoluteStoragePath))
            {
                Directory.CreateDirectory(absoluteStoragePath);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(absoluteStoragePath, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            if (isSecure)
            {
                // Store relative path for secure files
                return Path.Combine(storagePath, folderName, uniqueFileName).Replace('\\', '/');
            }
            
            var backendUrl = _configuration["BackendBaseUrl"] ?? "http://localhost:5159";
            return $"{backendUrl}/files/{folderName}/{uniqueFileName}";
        }

        public async Task<string> SaveFileAsync(byte[] content, string fileName, string folderName)
        {
            if (content == null || content.Length == 0)
                throw new ArgumentException("Content is empty");

            var webRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var isSecure = folderName.ToLower() == "videos" || folderName.ToLower() == "documents" || folderName.ToLower() == "pdfs";
            
            var storagePath = isSecure 
                ? _configuration["FileStorage:SecureMediaStoragePath"] ?? "wwwroot/secure_uploads"
                : _configuration["FileStorage:PublicMediaStoragePath"] ?? "wwwroot/uploads";

            var absoluteStoragePath = Path.Combine(Directory.GetCurrentDirectory(), storagePath, folderName);

            if (!Directory.Exists(absoluteStoragePath))
            {
                Directory.CreateDirectory(absoluteStoragePath);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(absoluteStoragePath, uniqueFileName);

            await File.WriteAllBytesAsync(filePath, content);

            if (isSecure)
            {
                return Path.Combine(storagePath, folderName, uniqueFileName).Replace('\\', '/');
            }

            var backendUrl = _configuration["BackendBaseUrl"] ?? "http://localhost:5159";
            return $"{backendUrl}/files/{folderName}/{uniqueFileName}";
        }

        public Task<bool> DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return Task.FromResult(false);

            var webRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            // Remove leading slashes/backslashes to avoid path traversal
            var normalizedPath = fileUrl.Replace('\\', '/').TrimStart('/');
            var fullPath = Path.Combine(webRoot, normalizedPath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<(Stream? Stream, string ContentType)> GetFileStreamAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return Task.FromResult<(Stream?, string)>((null, string.Empty));

            var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), fileUrl);

            if (!File.Exists(absolutePath))
                return Task.FromResult<(Stream?, string)>((null, string.Empty));

            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(absolutePath, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult<(Stream?, string)>((stream, contentType));
        }
    }
}
