using LMS.Core.Models;
using LMS.DAL.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface ICourseReviewRepository : IRepository<int, CourseReview>
    {
        Task<IEnumerable<CourseReview>> GetReviewsByCourseIdAsync(int courseId);
    }
}
