using LMS.Core.Models;
using LMS.DAL.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface ICertificateRepository : IRepository<int, Certificate>
    {
        Task<IEnumerable<Certificate>> GetCertificatesByUserIdAsync(int userId);
    }
}
