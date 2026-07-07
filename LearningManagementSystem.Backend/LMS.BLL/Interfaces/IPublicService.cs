using LMS.Core.DTOs.PublicDTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IPublicService
    {
        Task<LandingStatsResponse> GetLandingStatsAsync();
        Task<IEnumerable<TopInstructorResponse>> GetTopInstructorsAsync(int limit = 4);
    }
}
