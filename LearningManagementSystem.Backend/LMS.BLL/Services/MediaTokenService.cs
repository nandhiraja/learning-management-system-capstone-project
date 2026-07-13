using LMS.BLL.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using System;
using System.Text.Json;

namespace LMS.BLL.Services
{
    public class MediaTokenService : IMediaTokenService
    {
        private readonly ITimeLimitedDataProtector _protector;

        public MediaTokenService(IDataProtectionProvider provider)
        {
            // Create a protector specific to media tokens
            _protector = provider.CreateProtector("LMS.MediaTokens").ToTimeLimitedDataProtector();
        }

        public string GenerateToken(string filePath, Guid userId, TimeSpan? expiration = null)
        {
            var payload = new MediaTokenPayload
            {
                FilePath = filePath,
                UserId = userId
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            
            // Set token expiry to the provided expiration or default to 30 minutes
            var expiry = expiration ?? TimeSpan.FromMinutes(30);
            return _protector.Protect(jsonPayload, expiry);
        }

        public bool ValidateToken(string token, out string? filePath)
        {
            filePath = null;

            try
            {
                var jsonPayload = _protector.Unprotect(token);
                var payload = JsonSerializer.Deserialize<MediaTokenPayload>(jsonPayload);

                if (payload != null)
                {
                    filePath = payload.FilePath;
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                // Unprotect throws if the token is expired or tampered with
                return false;
            }
        }

        private class MediaTokenPayload
        {
            public string FilePath { get; set; } = string.Empty;
            public Guid UserId { get; set; }
        }
    }
}
