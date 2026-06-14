using LMS.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface ICourseSectionService
    {
        Task<CourseSectionResponse> CreateSectionAsync(Guid courseGuid, CourseSectionRequest request, Guid userGuid);
        Task<IEnumerable<CourseSectionResponse>> GetSectionsByCourseIdAsync(Guid courseGuid);
        Task<bool> UpdateSectionAsync(int sectionId, CourseSectionRequest request, Guid userGuid);
        Task<bool> DeleteSectionAsync(int sectionId, Guid userGuid);
    }
}
