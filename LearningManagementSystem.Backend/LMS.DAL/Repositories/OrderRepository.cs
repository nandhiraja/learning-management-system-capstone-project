using LMS.BLL.Interfaces;
using LMS.Core.Models;
using LMS.Core.Enums;
using LMS.DAL.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LMS.DAL.Repositories
{
    public class OrderRepository : Repository<int, Order>, IOrderRepository
    {
        public OrderRepository(LMSDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(int userId)
        {
            return await _context.Orders
                .Where(o => o.UserId == userId)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderWithDetailsAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Course)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Order?> GetByExternalIdAsync(Guid externalId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Course)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.ExternalId == externalId);
        }

        public async Task<decimal> GetRevenueByCourseIdsAsync(IEnumerable<int> courseIds)
        {
            return await _context.OrderItems
                .Where(oi => courseIds.Contains(oi.CourseId) && oi.Order.Status == OrderStatus.Completed)
                .SumAsync(oi => oi.FinalPrice);
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _context.Orders
                .Where(o => o.Status == OrderStatus.Completed)
                .SumAsync(o => o.Amount);
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.Orders.CountAsync();
        }

        public async Task<IEnumerable<OrderItem>> GetAllCompletedOrderItemsAsync()
        {
            return await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.Status == OrderStatus.Completed)
                .ToListAsync();
        }
    }
}
