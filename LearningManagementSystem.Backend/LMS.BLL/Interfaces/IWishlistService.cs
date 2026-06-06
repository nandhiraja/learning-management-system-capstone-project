using LMS.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IWishlistService
    {
        Task<bool> AddToWishlistAsync(Guid userGuid, int courseId);
        Task<IEnumerable<CourseResponse>> GetWishlistAsync(Guid userGuid);
        Task<bool> RemoveFromWishlistAsync(Guid userGuid, int courseId);
    }
}
