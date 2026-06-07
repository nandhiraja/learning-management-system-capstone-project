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

        public UserService(IUserRepository userRepository, IRoleRepository roleRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _mapper = mapper;
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
            if (!string.IsNullOrEmpty(request.ProfilePictureUrl))
            {
                user.ProfilePictureUrl = request.ProfilePictureUrl;
            }
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

            var roles = await _roleRepository.GetAllAsync();
            var instructorRole = roles.FirstOrDefault(r => r.Name.Equals("Instructor", StringComparison.OrdinalIgnoreCase));
            if (instructorRole == null)
            {
                throw new NotFoundException("Role", "Instructor");
            }

            user.RoleId = instructorRole.Id;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.Update(user);
            return true;
        }
    }
}