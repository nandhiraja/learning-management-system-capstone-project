using LMS.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface ICourseService
    {
        Task<(IEnumerable<CourseResponse> Items, int TotalCount)> GetCoursesAsync(int page, int pageSize, int? categoryId, string? search, decimal? minPrice, decimal? maxPrice, string? language, string? sortBy);
        Task<CourseResponse?> GetCourseByIdAsync(Guid courseGuid, Guid? userGuid = null);
        Task<CourseResponse> CreateCourseAsync(CourseCreateRequest request, Guid instructorGuid);
        Task<CourseUpdateResult> UpdateCourseAsync(Guid courseGuid, CourseUpdateRequest request, Guid userGuid);
        Task<bool> DeleteCourseAsync(Guid courseGuid, Guid userGuid);
        Task<bool> SubmitForReviewAsync(Guid courseGuid, Guid userGuid);
        Task<bool> PublishCourseAsync(Guid courseGuid);
        Task<bool> RejectCourseAsync(Guid courseGuid, string reason);
        Task<bool> UploadThumbnailAsync(Guid courseGuid, string fileUrl, Guid userGuid);
        Task<bool> ArchiveCourseAsync(Guid courseGuid, Guid userGuid, bool isAdmin, string reason);
    }
}
