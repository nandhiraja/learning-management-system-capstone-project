using LMS.BLL.Interfaces;
using LMS.Core.Models;
using LMS.DAL.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LMS.DAL.Repositories
{
    public class QuizOptionRepository : Repository<int, QuizOption>, IQuizOptionRepository
    {
        public QuizOptionRepository(LMSDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<QuizOption>> GetOptionsByQuestionIdAsync(int questionId)
        {
            return await _context.QuizOptions
                .Where(o => o.QuizQuestionId == questionId)
                .ToListAsync();
        }
    }
}
