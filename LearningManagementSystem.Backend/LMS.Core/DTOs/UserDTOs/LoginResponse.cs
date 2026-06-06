using System;
using System.Collections.Generic;

namespace LMS.Core.DTOs
{
    public class LoginResponse
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public int ExpiresIn { get; set; }
        public UserLoginInfo User { get; set; } = null!;
    }

    public class UserLoginInfo
    {
        public Guid UserGuid { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public List<string> Roles { get; set; } = new List<string>();
    }
}