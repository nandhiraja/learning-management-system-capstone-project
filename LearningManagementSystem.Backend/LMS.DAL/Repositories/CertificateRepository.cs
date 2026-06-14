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

        public override async Task<Certificate?> Get(int id)
        {
            return await _context.Certificates
                .Include(c => c.User)
                .Include(c => c.Course)
                    .ThenInclude(co => co.Instructor)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Certificate>> GetCertificatesByUserIdAsync(int userId)
        {
            return await _context.Certificates
                .Where(c => c.UserId == userId)
                .Include(c => c.User)
                .Include(c => c.Course)
                    .ThenInclude(co => co.Instructor)
                .ToListAsync();
        }
    }
}
