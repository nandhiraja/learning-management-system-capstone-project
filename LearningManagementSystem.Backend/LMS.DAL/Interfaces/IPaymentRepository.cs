using LMS.Core.Models;
using LMS.DAL.Interfaces;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IPaymentRepository : IRepository<int, Payment>
    {
        Task<Payment?> GetPaymentByOrderIdAsync(int orderId);
    }
}
