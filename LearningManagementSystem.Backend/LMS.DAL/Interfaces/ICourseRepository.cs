using LMS.Core.Models;
using LMS.DAL.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface ICourseRepository : IRepository<int, Course>
    {
        Task<IEnumerable<Course>> GetCoursesWithDetailsAsync();
        Task<Course?> GetCourseWithDetailsAsync(int id);
        Task<Course?> GetByExternalIdAsync(Guid externalId);
        Task<int> GetCountAsync();
        Task<int> GetPendingCoursesCountAsync();
        Task<IEnumerable<Course>> GetPendingCoursesAsync();
    }
}

