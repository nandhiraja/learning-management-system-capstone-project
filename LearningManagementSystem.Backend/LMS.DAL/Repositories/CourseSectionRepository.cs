using LMS.BLL.Interfaces;
using LMS.Core.Models;
using LMS.DAL.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LMS.DAL.Repositories
{
    public class CourseSectionRepository : Repository<int, CourseSection>, ICourseSectionRepository
    {
        public CourseSectionRepository(LMSDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<CourseSection>> GetSectionsByCourseIdAsync(int courseId)
        {
            return await _context.CourseSections
                .Where(s => s.CourseId == courseId)
                .OrderBy(s => s.Order)
                .ToListAsync();
        }
    }
}
