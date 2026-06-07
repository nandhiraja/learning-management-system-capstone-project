using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using LMS.Core.Models;
using LMS.Core.Exception;

namespace LMS.BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IMapper _mapper;

        public AuthService(
            IUserRepository userRepository, 
            IRoleRepository roleRepository, 
            IJwtTokenGenerator jwtTokenGenerator,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _jwtTokenGenerator = jwtTokenGenerator;
            _mapper = mapper;
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await _userRepository.GetUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new ArgumentException("Email is already registered.");
            }

            // Fetch default role (e.g. Student role with ID 1 or name "Student")
            var defaultRole = await _roleRepository.Get(1);
            if (defaultRole == null)
            {
                // Fallback to find first role in db
                var roles = await _roleRepository.GetAllAsync();
                defaultRole = roles.FirstOrDefault();
            }

            if (defaultRole == null)
            {
                throw new InvalidOperationException("Default Student role is not configured in database.");
            }

            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                UserName = request.Email, // default username to email
                PhoneNo = request.PhoneNo,
                PasswordHash = HashPassword(request.Password),
                RoleId = defaultRole.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdUser = await _userRepository.Create(user);
            return _mapper.Map<RegisterResponse>(createdUser);
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetUserByEmailAsync(request.Email);
            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new ArgumentException("Invalid email or password.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Your account has been blocked by administrator.");
            }

            var accessToken = _jwtTokenGenerator.GenerateToken(user, new[] { user.Role?.Name ?? "User" });
            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.Update(user);

            return new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 3600, // 1 hour
                User = new UserLoginInfo
                {
                    UserGuid = user.ExternalId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = new List<string> { user.Role?.Name ?? "User" }
                }
            };
        }

        public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var user = await _userRepository.GetUserByRefreshTokenAsync(request.RefreshToken);
            if (user == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Your account has been blocked by administrator.");
            }

            var newAccessToken = _jwtTokenGenerator.GenerateToken(user, new[] { user.Role?.Name ?? "User" });
            var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.Update(user);

            return new LoginResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = 3600, // 1 hour
                User = new UserLoginInfo
                {
                    UserGuid = user.ExternalId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = new List<string> { user.Role?.Name ?? "User" }
                }
            };
        }

        public async Task LogoutAsync(string refreshToken)
        {
            var user = await _userRepository.GetUserByRefreshTokenAsync(refreshToken);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.Update(user);
            }
        }

        public Task ForgotPasswordAsync(string email)
        {
            throw new NotImplementedException("Password reset flows are currently not supported by the database schema.");
        }

        public Task ResetPasswordAsync(string token, string newPassword)
        {
            throw new NotImplementedException("Password reset flows are currently not supported by the database schema.");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            return HashPassword(password) == hashedPassword;
        }
    }
}
