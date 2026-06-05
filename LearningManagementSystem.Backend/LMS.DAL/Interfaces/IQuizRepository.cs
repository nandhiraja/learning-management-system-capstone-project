using LMS.Core.Models;
using LMS.DAL.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IQuizRepository : IRepository<int, Quiz>
    {
        Task<Quiz?> GetQuizWithQuestionsAsync(int id);
        Task<IEnumerable<Quiz>> GetQuizzesByCourseIdAsync(int courseId);
    }
}
