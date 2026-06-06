using LMS.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface ICourseReviewService
    {
        Task<ReviewResponse> AddReviewAsync(Guid courseGuid, Guid userGuid, ReviewRequest request);
        Task<IEnumerable<ReviewResponse>> GetReviewsByCourseAsync(Guid courseGuid);
        Task<bool> UpdateReviewAsync(int reviewId, Guid userGuid, ReviewRequest request);
        Task<bool> DeleteReviewAsync(int reviewId, Guid userGuid);
    }
}
