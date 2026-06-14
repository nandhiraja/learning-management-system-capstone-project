using LMS.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IDiscussionService
    {
        Task<DiscussionResponse> CreateDiscussionAsync(Guid courseGuid, Guid userGuid, DiscussionCreateRequest request);
        Task<IEnumerable<DiscussionResponse>> GetDiscussionsForCourseAsync(Guid courseGuid, Guid userGuid, int? lectureId = null);
        Task<DiscussionDetailResponse?> GetDiscussionDetailsAsync(Guid discussionGuid, Guid userGuid);
        Task<DiscussionReplyResponse> CreateReplyAsync(Guid discussionGuid, Guid userGuid, DiscussionReplyCreateRequest request);
        Task<bool> TogglePinReplyAsync(Guid replyGuid, Guid userGuid);
        Task<int> LikeReplyAsync(Guid replyGuid, Guid userGuid);
    }
}
