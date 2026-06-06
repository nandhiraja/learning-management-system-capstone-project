using LMS.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IUserService
    {
        Task<UserProfileResponse> GetProfileAsync(Guid userGuid);
        Task<bool> UpdateProfileAsync(Guid userGuid, UserEditRequest request);
        Task<UserProfileResponse?> GetUserByIdAsync(Guid userGuid);
        Task<IEnumerable<UserProfileResponse>> GetUsersAsync();
        Task<bool> BlockUserAsync(Guid userGuid);
        Task<bool> UnblockUserAsync(Guid userGuid);
        Task<bool> AssignRoleAsync(Guid userGuid, int roleId);
        Task<bool> UploadProfilePictureAsync(Guid userGuid, string fileUrl);
        Task<bool> BecomeInstructorAsync(Guid userGuid);
    }
}
