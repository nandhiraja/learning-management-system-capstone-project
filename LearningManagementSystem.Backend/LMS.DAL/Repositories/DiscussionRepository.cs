using LMS.BLL.Interfaces;
using LMS.Core.Models;
using LMS.DAL.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LMS.DAL.Repositories
{
    public class DiscussionRepository : Repository<int, Discussion>, IDiscussionRepository
    {
        public DiscussionRepository(LMSDBContext context) : base(context)
        {
        }

        public async Task<Discussion?> GetByExternalIdAsync(Guid externalId)
        {
            return await _context.Discussions
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.ExternalId == externalId);
        }

        public async Task<IEnumerable<Discussion>> GetDiscussionsByCourseIdAsync(int courseId)
        {
            return await _context.Discussions
                .Where(d => d.CourseId == courseId)
                .Include(d => d.User)
                    .ThenInclude(u => u.Role)
                .Include(d => d.Lecture)
                .Include(d => d.Replies)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<Discussion?> GetDiscussionWithRepliesAsync(Guid externalId)
        {
            return await _context.Discussions
                .Include(d => d.User)
                    .ThenInclude(u => u.Role)
                .Include(d => d.Lecture)
                .Include(d => d.Replies)
                    .ThenInclude(r => r.User)
                        .ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(d => d.ExternalId == externalId);
        }

        public async Task<DiscussionReply?> GetReplyByExternalIdAsync(Guid externalId)
        {
            return await _context.DiscussionReplies
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ExternalId == externalId);
        }

        public async Task<DiscussionReply> CreateReplyAsync(DiscussionReply reply)
        {
            await _context.DiscussionReplies.AddAsync(reply);
            await _context.SaveChangesAsync();
            return reply;
        }

        public async Task UpdateReplyAsync(DiscussionReply reply)
        {
            _context.DiscussionReplies.Update(reply);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Discussion>> GetDiscussionsByCourseIdsAsync(IEnumerable<int> courseIds)
        {
            return await _context.Discussions
                .Where(d => courseIds.Contains(d.CourseId))
                .Include(d => d.User)
                    .ThenInclude(u => u.Role)
                .Include(d => d.Course)
                .Include(d => d.Lecture)
                .Include(d => d.Replies)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }
    }
}
