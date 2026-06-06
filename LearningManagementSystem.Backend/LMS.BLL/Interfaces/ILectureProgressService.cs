using LMS.Core.DTOs;
using System;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface ILectureProgressService
    {
        Task<bool> UpdateProgressAsync(Guid userGuid, ProgressUpdateRequest request);
        Task<ProgressResponse> GetProgressAsync(Guid userGuid, int enrollmentId);
        Task<ProgressResponse> GetProgressByCourseAsync(Guid userGuid, Guid courseGuid);
    }
}
