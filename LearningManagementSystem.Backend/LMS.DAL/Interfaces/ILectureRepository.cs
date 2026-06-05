using LMS.Core.Models;
using LMS.DAL.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface ILectureRepository : IRepository<int, Lecture>
    {
        Task<IEnumerable<Lecture>> GetLecturesBySectionIdAsync(int sectionId);
    }
}
