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
    }
}
