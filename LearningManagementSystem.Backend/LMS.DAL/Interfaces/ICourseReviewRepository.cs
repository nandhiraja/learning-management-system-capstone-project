using LMS.Core.Models;
using LMS.DAL.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface ICourseReviewRepository : IRepository<int, CourseReview>
    {
        Task<IEnumerable<CourseReview>> GetReviewsByCourseIdAsync(int courseId);
        Task<(IEnumerable<CourseReview> Items, int TotalCount)> GetReviewsByCourseIdPaginatedAsync(int courseId, int page, int pageSize);
        Task<IEnumerable<CourseReview>> GetReviewsByCourseIdsAsync(IEnumerable<int> courseIds);
    }
}
