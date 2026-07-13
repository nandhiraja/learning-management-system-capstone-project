using System;

namespace LMS.BLL.Interfaces
{
    public interface IMediaTokenService
    {
        /// <summary>
        /// Generates a short-lived secure token for a specific file and user.
        /// </summary>
        string GenerateToken(string filePath, Guid userId, TimeSpan? expiration = null);

        /// <summary>
        /// Validates a token and returns true if valid. The out parameter contains the original file path.
        /// </summary>
        bool ValidateToken(string token, out string? filePath);
    }
}
