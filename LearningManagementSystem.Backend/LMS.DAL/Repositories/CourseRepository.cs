using LMS.BLL.Interfaces;
using LMS.Core.Models;
using LMS.DAL.Data;
using LMS.Core.Enums;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LMS.DAL.Repositories
{
    public class CourseRepository : Repository<int, Course>, ICourseRepository
    {
        public CourseRepository(LMSDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Course>> GetCoursesWithDetailsAsync()
        {
            return await _context.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Category)
                .Include(c => c.Language)
                .Include(c => c.CourseReviews)
                .ToListAsync();
        }

        public async Task<Course?> GetCourseWithDetailsAsync(int id)
        {
            return await _context.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Category)
                .Include(c => c.Language)
                .Include(c => c.Enrollments)
                .Include(c => c.Sections)
                    .ThenInclude(s => s.Lectures)
                        .ThenInclude(l => l.Quizzes)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Course?> GetByExternalIdAsync(Guid externalId)
        {
            return await _context.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Category)
                .Include(c => c.Language)
                .Include(c => c.Enrollments)
                .Include(c => c.Sections)
                    .ThenInclude(s => s.Lectures)
                        .ThenInclude(l => l.Quizzes)
                .FirstOrDefaultAsync(c => c.ExternalId == externalId);
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.Courses.CountAsync();
        }

        public async Task<int> GetPendingCoursesCountAsync()
        {
            return await _context.Courses.CountAsync(c => c.Status == CourseStatus.PendingReview);
        }

        public async Task<IEnumerable<Course>> GetPendingCoursesAsync()
        {
            return await _context.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Category)
                .Include(c => c.Language)
                .Where(c => c.Status == CourseStatus.PendingReview)
                .ToListAsync();
        }
    }
}
