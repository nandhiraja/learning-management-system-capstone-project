using LMS.BLL.Interfaces;
using LMS.Core.Models;
using LMS.DAL.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LMS.DAL.Repositories
{
    public class CourseReviewRepository : Repository<int, CourseReview>, ICourseReviewRepository
    {
        public CourseReviewRepository(LMSDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<CourseReview>> GetReviewsByCourseIdAsync(int courseId)
        {
            return await _context.CourseReviews
                .Where(r => r.CourseId == courseId)
                .Include(r => r.User)
                .ToListAsync();
        }

        public async Task<(IEnumerable<CourseReview> Items, int TotalCount)> GetReviewsByCourseIdPaginatedAsync(int courseId, int page, int pageSize)
        {
            var query = _context.CourseReviews
                .Where(r => r.CourseId == courseId);

            var totalCount = await query.CountAsync();

            var items = await query
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<CourseReview>> GetReviewsByCourseIdsAsync(IEnumerable<int> courseIds)
        {
            return await _context.CourseReviews
                .Where(r => courseIds.Contains(r.CourseId))
                .ToListAsync();
        }
    }
}
