using LMS.Core.Models;
using LMS.DAL.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IQuizOptionRepository : IRepository<int, QuizOption>
    {
        Task<IEnumerable<QuizOption>> GetOptionsByQuestionIdAsync(int questionId);
    }
}
