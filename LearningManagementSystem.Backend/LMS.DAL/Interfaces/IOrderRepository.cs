using LMS.Core.Models;
using LMS.DAL.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IOrderRepository : IRepository<int, Order>
    {
        Task<IEnumerable<Order>> GetOrdersByUserIdAsync(int userId);
        Task<Order?> GetOrderWithDetailsAsync(int id);
        Task<Order?> GetByExternalIdAsync(Guid externalId);
        Task<decimal> GetRevenueByCourseIdsAsync(IEnumerable<int> courseIds);
        Task<decimal> GetTotalRevenueAsync();
        Task<int> GetCountAsync();
    }
}
