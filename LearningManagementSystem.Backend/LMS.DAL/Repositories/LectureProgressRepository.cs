using LMS.BLL.Interfaces;
using LMS.Core.Models;
using LMS.DAL.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LMS.DAL.Repositories
{
    public class LectureProgressRepository : Repository<int, LectureProgress>, ILectureProgressRepository
    {
        public LectureProgressRepository(LMSDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<LectureProgress>> GetProgressByEnrollmentIdAsync(int enrollmentId)
        {
            return await _context.LectureProgresses
                .Where(lp => lp.EnrollmentId == enrollmentId)
                .ToListAsync();
        }

        public async Task<LectureProgress?> GetProgressByLectureAndEnrollmentAsync(int lectureId, int enrollmentId)
        {
            return await _context.LectureProgresses
                .FirstOrDefaultAsync(lp => lp.LectureId == lectureId && lp.EnrollmentId == enrollmentId);
        }
    }
}
