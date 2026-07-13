using AutoMapper;
using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using LMS.Core.Models;

namespace LMS.BLL.Mappers
{
    public class LectureResponseSecureMediaMappingAction : IMappingAction<Lecture, LectureResponse>
    {
        private readonly IMediaTokenService _mediaTokenService;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public LectureResponseSecureMediaMappingAction(IMediaTokenService mediaTokenService, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _mediaTokenService = mediaTokenService;
            _configuration = configuration;
        }

        public void Process(Lecture source, LectureResponse destination, ResolutionContext context)
        {
            if (source.ContentType == LMS.Core.Enums.ContentType.Video || source.ContentType == LMS.Core.Enums.ContentType.pdf)
            {
                if (!string.IsNullOrEmpty(source.ContentUrl) && source.ContentUrl.Contains("secure_uploads"))
                {
                    // Generate a token for the actual physical/relative path
                    var expiry = source.ContentType == LMS.Core.Enums.ContentType.pdf 
                        ? System.TimeSpan.FromMinutes(2) 
                        : System.TimeSpan.FromMinutes(30);
                        
                    var token = _mediaTokenService.GenerateToken(source.ContentUrl, System.Guid.Empty, expiry);
                    destination.MediaAuthToken = token;
                    
                    var backendUrl = _configuration["BackendBaseUrl"] ?? "http://localhost:5159";
                    destination.ContentUrl = $"{backendUrl}/api/media/stream/{source.ContentUrl}";
                }
            }
        }
    }
}
