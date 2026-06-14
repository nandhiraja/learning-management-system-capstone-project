using LMS.Core.Models;
using LMS.DAL.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IDiscussionRepository : IRepository<int, Discussion>
    {
        Task<Discussion?> GetByExternalIdAsync(Guid externalId);
        Task<IEnumerable<Discussion>> GetDiscussionsByCourseIdAsync(int courseId);
        Task<Discussion?> GetDiscussionWithRepliesAsync(Guid externalId);
        Task<DiscussionReply?> GetReplyByExternalIdAsync(Guid externalId);
        Task<DiscussionReply> CreateReplyAsync(DiscussionReply reply);
        Task UpdateReplyAsync(DiscussionReply reply);
        Task<IEnumerable<Discussion>> GetDiscussionsByCourseIdsAsync(IEnumerable<int> courseIds);
    }
}
