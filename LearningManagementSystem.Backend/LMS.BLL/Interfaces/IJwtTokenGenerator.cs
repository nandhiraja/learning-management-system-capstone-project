using LMS.Core.Models;
using System.Collections.Generic;

namespace LMS.BLL.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user, IEnumerable<string> roles);
        string GenerateRefreshToken();
    }
}
