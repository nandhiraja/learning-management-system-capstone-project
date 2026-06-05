using LMS.BLL.Interfaces;
using LMS.Core.Models;
using LMS.DAL.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LMS.DAL.Repositories
{
    public class QuizQuestionRepository : Repository<int, QuizQuestion>, IQuizQuestionRepository
    {
        public QuizQuestionRepository(LMSDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<QuizQuestion>> GetQuestionsByQuizIdAsync(int quizId)
        {
            return await _context.QuizQuestions
                .Where(q => q.QuizId == quizId)
                .Include(q => q.Options)
                .ToListAsync();
        }
    }
}
