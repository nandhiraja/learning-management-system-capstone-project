using LMS.BLL.Interfaces;
using LMS.Core.Models;
using LMS.DAL.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LMS.DAL.Repositories
{
    public class CertificateRepository : Repository<int, Certificate>, ICertificateRepository
    {
        public CertificateRepository(LMSDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Certificate>> GetCertificatesByUserIdAsync(int userId)
        {
            return await _context.Certificates
                .Where(c => c.UserId == userId)
                .Include(c => c.Course)
                .ToListAsync();
        }
    }
}
