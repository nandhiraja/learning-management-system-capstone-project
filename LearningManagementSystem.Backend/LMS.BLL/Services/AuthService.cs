using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using LMS.Core.Models;
using LMS.Core.Exception;
using LMS.DAL.Data;

namespace LMS.BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly LMSDBContext _context;
        private readonly IMapper _mapper;

        public AuthService(
            IUserRepository userRepository, 
            IRoleRepository roleRepository, 
            IJwtTokenGenerator jwtTokenGenerator,
            LMSDBContext context,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _jwtTokenGenerator = jwtTokenGenerator;
            _context = context;
            _mapper = mapper;
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            var allUsers = await _userRepository.GetAllAsync() ?? Enumerable.Empty<User>();

            if (allUsers.Any(u => !string.IsNullOrEmpty(u.Email) && u.Email.Trim().Equals(request.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Email is already registered.");
            }

            if (allUsers.Any(u => !string.IsNullOrEmpty(u.UserName) && u.UserName.Trim().Equals(request.UserName.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Username is already registered.");
            }

            if (allUsers.Any(u => !string.IsNullOrEmpty(u.PhoneNo) && u.PhoneNo.Trim().Equals(request.PhoneNo.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Phone number is already registered.");
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
                UserName = request.UserName,
                PhoneNo = request.PhoneNo,
                PasswordHash = HashPassword(request.Password),
                RoleId = defaultRole.Id,
                IsActive = true,
                CertificateName = $"{request.FirstName} {request.LastName}".Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdUser = await _userRepository.Create(user);
            return _mapper.Map<RegisterResponse>(createdUser);
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _userRepository.GetUserByEmailAsync(request.Email);
                if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
                {
                    throw new ArgumentException("Invalid email or password.");
                }

                if (!user.IsActive)
                {
                    throw new UnauthorizedAccessException("Your account has been blocked.");
                }

                var accessToken = _jwtTokenGenerator.GenerateToken(user, new[] { user.Role?.Name ?? "User" });
                var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

                var now = DateTime.UtcNow;
                var expired = user.RefreshTokens.Where(t => t.ExpiryTime < now).ToList();
                foreach (var exp in expired)
                {
                    user.RefreshTokens.Remove(exp);
                }

                user.RefreshTokens.Add(new UserRefreshToken
                {
                    Token = refreshToken,
                    ExpiryTime = now.AddDays(7)
                });
                user.UpdatedAt = now;
                await _userRepository.Update(user);

                await dbTransaction.CommitAsync();

                return new LoginResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = 3600, // 1 hour
                };
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _userRepository.GetUserByRefreshTokenAsync(request.RefreshToken);
                var existingToken = user?.RefreshTokens.FirstOrDefault(t => t.Token == request.RefreshToken);
                if (user == null || existingToken == null || existingToken.ExpiryTime < DateTime.UtcNow)
                {
                    throw new UnauthorizedAccessException("Invalid or expired refresh token.");
                }

                if (!user.IsActive)
                {
                    throw new UnauthorizedAccessException("Your account has been blocked.");
                }

                var newAccessToken = _jwtTokenGenerator.GenerateToken(user, new[] { user.Role?.Name ?? "User" });
                var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();

                existingToken.Token = newRefreshToken;
                existingToken.ExpiryTime = DateTime.UtcNow.AddDays(7);
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.Update(user);

                await dbTransaction.CommitAsync();

                return new LoginResponse
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresIn = 3600, // 1 hour
                };
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task LogoutAsync(string refreshToken)
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _userRepository.GetUserByRefreshTokenAsync(refreshToken);
                if (user != null)
                {
                    var existingToken = user.RefreshTokens.FirstOrDefault(t => t.Token == refreshToken);
                    if (existingToken != null)
                    {
                        user.RefreshTokens.Remove(existingToken);
                        user.UpdatedAt = DateTime.UtcNow;
                        await _userRepository.Update(user);
                    }
                }
                await dbTransaction.CommitAsync();
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
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
