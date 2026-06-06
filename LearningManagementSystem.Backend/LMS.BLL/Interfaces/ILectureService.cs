using LMS.Core.DTOs;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface ILectureService
    {
        Task<LectureResponse> CreateLectureAsync(int sectionId, LectureRequest request);
        Task<LectureResponse?> GetLectureByIdAsync(int lectureId, Guid userGuid);
        Task<bool> UpdateLectureAsync(int lectureId, LectureRequest request);
        Task<bool> DeleteLectureAsync(int lectureId);
        Task<bool> UploadLectureContentAsync(int lectureId, string fileUrl);
    }
}
