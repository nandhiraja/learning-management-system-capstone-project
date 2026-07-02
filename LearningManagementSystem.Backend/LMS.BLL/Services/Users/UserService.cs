using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using LMS.Core.Models;
using LMS.Core.Exception;
using Microsoft.AspNetCore.WebUtilities;

namespace LMS.BLL.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;

        public UserService(IUserRepository userRepository, IRoleRepository roleRepository, IMapper mapper, INotificationService notificationService)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        public async Task<bool> AssignRoleAsync(Guid userGuid, int roleId)
        {
            Role? role = await _roleRepository.Get(roleId);
            if (role == null)  
                throw new NotFoundException(nameof(Role), roleId);

            User? user = await _userRepository.Get(userGuid);
            if (user == null)  
                throw new NotFoundException(nameof(User), userGuid);
 
            user.RoleId = roleId;
            await _userRepository.Update(user);
            return true;
        }

        public async Task<bool> BlockUserAsync(Guid userGuid)
        {
            User? user = await _userRepository.Get(userGuid);
            if (user == null)  
                throw new NotFoundException(nameof(User), userGuid);
 
            user.IsActive = false;
            await _userRepository.Update(user);

            string fullName = ($"{user.FirstName} {user.LastName}").Trim();
            string emailBody = $@"
                <h2>Account Blocked</h2>
                <p>Dear {fullName},</p>
                <p>Your LearnHub account has been blocked by an administrator. You will not be able to log in or access platform services.</p>
                <p>Best regards,<br/>LMS Team</p>";
            await _notificationService.SendEmailAsync(user.Email, "Account Blocked", emailBody);

            return true;
        }

        public async Task<UserProfileResponse> GetProfileAsync(Guid userGuid)
        {
            User? user = await _userRepository.Get(userGuid);
            if (user == null)  
                throw new NotFoundException(nameof(User), userGuid);
            
            return _mapper.Map<UserProfileResponse>(user);
        }

        public async Task<UserProfileResponse?> GetUserByIdAsync(Guid userGuid)
        {
            User? user = await _userRepository.Get(userGuid);
            if (user == null) return null;
            return _mapper.Map<UserProfileResponse>(user);
        }

        public async Task<IEnumerable<UserProfileResponse>> GetUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<UserProfileResponse>>(users);
        }

        public async Task<bool> UnblockUserAsync(Guid userGuid)
        {
            User? user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            user.IsActive = true;
            await _userRepository.Update(user);

            string fullName = ($"{user.FirstName} {user.LastName}").Trim();
            string emailBody = $@"
                <h2>Account Restored</h2>
                <p>Dear {fullName},</p>
                <p>Your LearnHub account has been unblocked. You can now log in and access all your courses.</p>
                <p>Best regards,<br/>LMS Team</p>";
            await _notificationService.SendEmailAsync(user.Email, "Account Restored", emailBody);

            return true;
        }

        public async Task<bool> UpdateProfileAsync(Guid userGuid, UserEditRequest request)
        {
            User? user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNo = request.PhoneNo;
            user.ProfilePictureUrl = request.ProfilePictureUrl;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.Update(user);
            return true;
        }

        public async Task<bool> UploadProfilePictureAsync(Guid userGuid, string fileUrl)
        {
            User? user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            user.ProfilePictureUrl = fileUrl;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.Update(user);
            return true;
        }

        public async Task<bool> BecomeInstructorAsync(Guid userGuid)
        {
            User? user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            user.InstructorRequestPending = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.Update(user);
            return true;
        }

        public async Task<IEnumerable<UserProfileResponse>> GetPendingInstructorsAsync()
        {
            var users = await _userRepository.GetAllAsync();
            var pending = users.Where(u => u.InstructorRequestPending).ToList();
            return _mapper.Map<IEnumerable<UserProfileResponse>>(pending);
        }

        public async Task<bool> ApproveInstructorAsync(Guid userGuid)
        {
            User? user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var roles = await _roleRepository.GetAllAsync();
            var instructorRole = roles.FirstOrDefault(r => r.Name.Equals("Instructor", StringComparison.OrdinalIgnoreCase));
            if (instructorRole == null)
            {
                throw new NotFoundException("Role", "Instructor");
            }

            user.RoleId = instructorRole.Id;
            user.InstructorRequestPending = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.Update(user);

            string fullName = ($"{user.FirstName} {user.LastName}").Trim();
            string emailBody = $@"
                <h2>Application Approved</h2>
                <p>Dear {fullName},</p>
                <p>We are pleased to inform you that your application to become an instructor on LMS has been approved. You now have access to instructor features.</p>
                <p>Best regards,<br/>LMS Team</p>";
            await _notificationService.SendEmailAsync(user.Email, "Instructor Application Approved", emailBody);

            return true;
        }

        public async Task<bool> RejectInstructorAsync(Guid userGuid)
        {
            User? user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            user.InstructorRequestPending = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.Update(user);

            string fullName = ($"{user.FirstName} {user.LastName}").Trim();
            string emailBody = $@"
                <h2>Application Rejected</h2>
                <p>Dear {fullName},</p>
                <p>We regret to inform you that your application to become an instructor has been rejected. Please update your profile/details and try again.</p>
                <p>Best regards,<br/>LMS Team</p>";
            await _notificationService.SendEmailAsync(user.Email, "Instructor Application Rejected", emailBody);

            return true;
        }

        public async Task<bool> DemoteToStudentAsync(Guid userGuid)
        {
            User? user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var roles = await _roleRepository.GetAllAsync();
            var studentRole = roles.FirstOrDefault(r => r.Name.Equals("Student", StringComparison.OrdinalIgnoreCase));
            if (studentRole == null)
            {
                throw new NotFoundException("Role", "Student");
            }

            user.RoleId = studentRole.Id;
            user.InstructorRequestPending = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.Update(user);

            string fullName = ($"{user.FirstName} {user.LastName}").Trim();
            string emailBody = $@"
                <h2>Account Role Demoted</h2>
                <p>Dear {fullName},</p>
                <p>Your account role has been demoted back to a Student. If you believe this is in error, please contact administration.</p>
                <p>Best regards,<br/>LMS Team</p>";
            await _notificationService.SendEmailAsync(user.Email, "Account Role Demoted", emailBody);

            return true;
        }
    }
}