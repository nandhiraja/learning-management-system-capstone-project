using LMS.Core.Models;
using LMS.DAL.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface ICourseSectionRepository : IRepository<int, CourseSection>
    {
        Task<IEnumerable<CourseSection>> GetSectionsByCourseIdAsync(int courseId);
    }
}
