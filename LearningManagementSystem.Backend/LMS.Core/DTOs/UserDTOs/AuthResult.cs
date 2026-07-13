namespace LMS.Core.DTOs
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public LoginResponse? Data { get; set; }

        public static AuthResult Ok(LoginResponse data) => new AuthResult { Success = true, Data = data };
        public static AuthResult Fail(string message) => new AuthResult { Success = false, ErrorMessage = message };
    }
}
