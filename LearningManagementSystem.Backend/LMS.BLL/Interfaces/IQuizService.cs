using LMS.Core.DTOs;
using System;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IQuizService
    {
        Task<QuizResponse> CreateQuizAsync(int lectureId, QuizRequest request, Guid userGuid);
        Task<QuizResponse?> GetQuizByIdAsync(int quizId);
        Task<bool> UpdateQuizAsync(int quizId, QuizRequest request, Guid userGuid);
        Task<bool> DeleteQuizAsync(int quizId, Guid userGuid);
        Task<QuizQuestionResponse> AddQuestionToQuizAsync(int quizId, QuizQuestionRequest request, Guid userGuid);
        Task<bool> UpdateQuestionAsync(int questionId, QuizQuestionRequest request, Guid userGuid);
        Task<bool> DeleteQuestionAsync(int questionId, Guid userGuid);
        Task<QuizSubmitResponse> SubmitQuizAnswersAsync(int quizId, Guid userGuid, QuizSubmitRequest request);
        Task<QuizProgressResponse> GetQuizProgressAsync(int quizId, Guid userGuid);
    }
}
