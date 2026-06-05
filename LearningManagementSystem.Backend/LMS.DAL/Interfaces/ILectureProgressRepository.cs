using LMS.Core.Models;
using LMS.DAL.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface ILectureProgressRepository : IRepository<int, LectureProgress>
    {
        Task<IEnumerable<LectureProgress>> GetProgressByEnrollmentIdAsync(int enrollmentId);
        Task<LectureProgress?> GetProgressByLectureAndEnrollmentAsync(int lectureId, int enrollmentId);
    }
}
