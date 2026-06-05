using LMS.Core.Models;
using LMS.DAL.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IEnrollmentRepository : IRepository<int, Enrollment>
    {
        Task<IEnumerable<Enrollment>> GetEnrollmentsByUserIdAsync(int userId);
        Task<Enrollment?> GetEnrollmentWithDetailsAsync(int id);
    }
}
