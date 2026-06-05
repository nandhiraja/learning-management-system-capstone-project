using LMS.BLL.Interfaces;
using LMS.Core.Models;
using LMS.DAL.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LMS.DAL.Repositories
{
    public class EnrollmentRepository : Repository<int, Enrollment>, IEnrollmentRepository
    {
        public EnrollmentRepository(LMSDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Enrollment>> GetEnrollmentsByUserIdAsync(int userId)
        {
            return await _context.Enrollments
                .Where(e => e.UserId == userId)
                .Include(e => e.Course)
                .ToListAsync();
        }

        public async Task<Enrollment?> GetEnrollmentWithDetailsAsync(int id)
        {
            return await _context.Enrollments
                .Include(e => e.Course)
                .Include(e => e.User)
                .Include(e => e.Certificate)
                .Include(e => e.LectureProgresses)
                .FirstOrDefaultAsync(e => e.Id == id);
        }
    }
}
