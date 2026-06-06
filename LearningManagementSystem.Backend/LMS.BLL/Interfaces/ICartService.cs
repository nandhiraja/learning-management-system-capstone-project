using LMS.Core.DTOs;
using System;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface ICartService
    {
        Task<CartResponse> GetCartAsync(Guid userGuid);
        Task<bool> AddToCartAsync(Guid userGuid, int courseId);
        Task<bool> RemoveFromCartAsync(Guid userGuid, int courseId);
        Task<bool> ClearCartAsync(Guid userGuid);
    }
}
