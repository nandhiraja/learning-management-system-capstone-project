using LMS.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponse> CreateOrderAsync(Guid userGuid, OrderCreateRequest request);
        Task<OrderResponse?> GetOrderByIdAsync(Guid orderGuid);
        Task<IEnumerable<OrderResponse>> GetUserOrdersAsync(Guid userGuid);
    }
}
