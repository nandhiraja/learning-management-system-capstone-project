using LMS.Core.Models;
using LMS.DAL.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IQuizQuestionRepository : IRepository<int, QuizQuestion>
    {
        Task<IEnumerable<QuizQuestion>> GetQuestionsByQuizIdAsync(int quizId);
    }
}
