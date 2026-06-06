using LMS.Core.DTOs;
using System;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IQuizService
    {
        Task<QuizResponse> CreateQuizAsync(int lectureId, QuizRequest request);
        Task<QuizResponse?> GetQuizByIdAsync(int quizId);
        Task<bool> UpdateQuizAsync(int quizId, QuizRequest request);
        Task<bool> DeleteQuizAsync(int quizId);
        Task<QuizQuestionResponse> AddQuestionToQuizAsync(int quizId, QuizQuestionRequest request);
        Task<bool> UpdateQuestionAsync(int questionId, QuizQuestionRequest request);
        Task<bool> DeleteQuestionAsync(int questionId);
        Task<QuizSubmitResponse> SubmitQuizAnswersAsync(int quizId, Guid userGuid, QuizSubmitRequest request);
    }
}
