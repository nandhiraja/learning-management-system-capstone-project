using LMS.BLL.Interfaces;
using LMS.Core.Models;
using LMS.DAL.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LMS.DAL.Repositories
{
    public class LectureRepository : Repository<int, Lecture>, ILectureRepository
    {
        public LectureRepository(LMSDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Lecture>> GetLecturesBySectionIdAsync(int sectionId)
        {
            return await _context.Lectures
                .Include(l => l.Quizzes)
                .Where(l => l.CourseSectionId == sectionId)
                .ToListAsync();
        }

        public async Task<Lecture?> GetLectureWithDetailsAsync(int lectureId)
        {
            return await _context.Lectures
                .Include(l => l.Quizzes)
                .Include(l => l.CourseSection)
                    .ThenInclude(s => s.Course)
                        .ThenInclude(c => c.Enrollments)
                .Include(l => l.CourseSection)
                    .ThenInclude(s => s.Course)
                        .ThenInclude(c => c.Instructor)
                .FirstOrDefaultAsync(l => l.Id == lectureId);
        }
    }
}

