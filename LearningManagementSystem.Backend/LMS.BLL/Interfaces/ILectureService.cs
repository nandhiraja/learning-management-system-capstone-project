using LMS.Core.DTOs;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface ILectureService
    {
        Task<LectureResponse> CreateLectureAsync(int sectionId, LectureRequest request, System.Guid userGuid);
        Task<LectureResponse?> GetLectureByIdAsync(int lectureId, System.Guid userGuid);
        Task<bool> UpdateLectureAsync(int lectureId, LectureRequest request, System.Guid userGuid);
        Task<bool> DeleteLectureAsync(int lectureId, System.Guid userGuid);
        Task<bool> UploadLectureContentAsync(int lectureId, string fileUrl, System.Guid userGuid);
    }
}
