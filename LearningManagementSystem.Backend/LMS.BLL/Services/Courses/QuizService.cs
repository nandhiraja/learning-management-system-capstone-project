using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using LMS.Core.Models;
using LMS.Core.Enums;
using LMS.Core.Exception;
using LMS.DAL.Interfaces;

namespace LMS.BLL.Services
{
    public class QuizService : IQuizService
    {
        private readonly IQuizRepository _quizRepository;
        private readonly IQuizQuestionRepository _questionRepository;
        private readonly IQuizOptionRepository _optionRepository;
        private readonly ILectureRepository _lectureRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public QuizService(
            IQuizRepository quizRepository,
            IQuizQuestionRepository questionRepository,
            IQuizOptionRepository optionRepository,
            ILectureRepository lectureRepository,
            IUserRepository userRepository,
            IMapper mapper)
        {
            _quizRepository = quizRepository;
            _questionRepository = questionRepository;
            _optionRepository = optionRepository;
            _lectureRepository = lectureRepository;
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<QuizResponse> CreateQuizAsync(int lectureId, QuizRequest request)
        {
            var lecture = await _lectureRepository.GetLectureWithDetailsAsync(lectureId);
            if (lecture == null)
            {
                throw new NotFoundException("Lecture", lectureId);
            }

            var quiz = new Quiz
            {
                Title = request.Title,
                PassScore = request.PassScore,
                TotalMarks = 100, // standard default
                LectureId = lectureId,
                CourseId = lecture.CourseSection?.CourseId ?? 0,
                MaxAttempts = 3,
                CurrentAttempt = 0,
                Status = QuizStatus.NotStarted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdQuiz = await _quizRepository.Create(quiz);

            var quizQuestions = new List<QuizQuestion>();
            if (request.Questions != null)
            {
                foreach (var qReq in request.Questions)
                {
                    var question = new QuizQuestion
                    {
                        QuizId = createdQuiz.Id,
                        QuestionText = qReq.QuestionText
                    };
                    var createdQuestion = await _questionRepository.Create(question);

                    var optionsList = new List<QuizOption>();
                    if (qReq.Options != null)
                    {
                        foreach (var optReq in qReq.Options)
                        {
                            var opt = new QuizOption
                            {
                                QuizQuestionId = createdQuestion.Id,
                                OptionText = optReq.OptionText,
                                IsCorrect = optReq.IsCorrect
                            };
                            var createdOpt = await _optionRepository.Create(opt);
                            optionsList.Add(createdOpt);
                        }
                    }
                    createdQuestion.Options = optionsList;
                    quizQuestions.Add(createdQuestion);
                }
            }

            createdQuiz.Questions = quizQuestions;

            var resp = _mapper.Map<QuizResponse>(createdQuiz);
            resp.Questions = _mapper.Map<IEnumerable<QuizQuestionResponse>>(quizQuestions);
            return resp;
        }

        public async Task<QuizResponse?> GetQuizByIdAsync(int quizId)
        {
            var quiz = await _quizRepository.Get(quizId);
            if (quiz == null) return null;

            // Load questions and options
            var questions = await GetQuestionsForQuizAsync(quizId);
            var resp = _mapper.Map<QuizResponse>(quiz);
            resp.Questions = _mapper.Map<IEnumerable<QuizQuestionResponse>>(questions);
            return resp;
        }

        public async Task<bool> UpdateQuizAsync(int quizId, QuizRequest request)
        {
            var quiz = await _quizRepository.Get(quizId);
            if (quiz == null) return false;

            quiz.Title = request.Title;
            quiz.PassScore = request.PassScore;
            quiz.UpdatedAt = DateTime.UtcNow;

            await _quizRepository.Update(quiz);
            return true;
        }

        public async Task<bool> DeleteQuizAsync(int quizId)
        {
            var quiz = await _quizRepository.Get(quizId);
            if (quiz == null) return false;

            await _quizRepository.Delete(quiz);
            return true;
        }

        public async Task<QuizQuestionResponse> AddQuestionToQuizAsync(int quizId, QuizQuestionRequest request)
        {
            var quiz = await _quizRepository.Get(quizId);
            if (quiz == null)
            {
                throw new NotFoundException("Quiz", quizId);
            }

            var question = new QuizQuestion
            {
                QuizId = quizId,
                QuestionText = request.QuestionText
            };

            var createdQuestion = await _questionRepository.Create(question);

            // Create options
            var optionsList = new List<QuizOption>();
            foreach (var optReq in request.Options)
            {
                var opt = new QuizOption
                {
                    QuizQuestionId = createdQuestion.Id,
                    OptionText = optReq.OptionText,
                    IsCorrect = optReq.IsCorrect
                };
                var createdOpt = await _optionRepository.Create(opt);
                optionsList.Add(createdOpt);
            }

            createdQuestion.Options = optionsList;
            var resp = _mapper.Map<QuizQuestionResponse>(createdQuestion);
            return resp;
        }

        public async Task<bool> UpdateQuestionAsync(int questionId, QuizQuestionRequest request)
        {
            var question = await _questionRepository.Get(questionId);
            if (question == null) return false;

            question.QuestionText = request.QuestionText;
            await _questionRepository.Update(question);

            // Re-create options: delete old, add new
            var oldOptions = await GetOptionsForQuestionAsync(questionId);
            foreach (var opt in oldOptions)
            {
                await _optionRepository.Delete(opt);
            }

            foreach (var optReq in request.Options)
            {
                var opt = new QuizOption
                {
                    QuizQuestionId = questionId,
                    OptionText = optReq.OptionText,
                    IsCorrect = optReq.IsCorrect
                };
                await _optionRepository.Create(opt);
            }

            return true;
        }

        public async Task<bool> DeleteQuestionAsync(int questionId)
        {
            var question = await _questionRepository.Get(questionId);
            if (question == null) return false;

            await _questionRepository.Delete(question);
            return true;
        }

        public async Task<QuizSubmitResponse> SubmitQuizAnswersAsync(int quizId, Guid userGuid, QuizSubmitRequest request)
        {
            var quiz = await _quizRepository.Get(quizId);
            if (quiz == null)
            {
                throw new NotFoundException("Quiz", quizId);
            }

            var user = await _userRepository.Get(userGuid);
            if (user == null)
            {
                throw new NotFoundException("User", userGuid);
            }

            var questions = await GetQuestionsForQuizAsync(quizId);
            int totalQuestions = questions.Count;
            if (totalQuestions == 0)
            {
                return new QuizSubmitResponse { Score = 0, Passed = false };
            }

            int correctAnswers = 0;
            foreach (var answer in request.Answers)
            {
                var question = questions.FirstOrDefault(q => q.Id == answer.QuestionId);
                if (question != null)
                {
                    var selectedOption = question.Options.FirstOrDefault(o => o.Id == answer.OptionId);
                    if (selectedOption != null && selectedOption.IsCorrect)
                    {
                        correctAnswers++;
                    }
                }
            }

            int score = (correctAnswers * 100) / totalQuestions;
            bool passed = score >= quiz.PassScore; // passing marks field maps to DTO PassScore

            return new QuizSubmitResponse
            {
                Score = score,
                Passed = passed
            };
        }

        // Helper private queries
        private async Task<List<QuizQuestion>> GetQuestionsForQuizAsync(int quizId)
        {
            var questions = await _questionRepository.GetQuestionsByQuizIdAsync(quizId);
            var list = questions.ToList();
            foreach (var q in list)
            {
                var options = await _optionRepository.GetOptionsByQuestionIdAsync(q.Id);
                q.Options = options.ToList();
            }
            return list;
        }

        private async Task<List<QuizOption>> GetOptionsForQuestionAsync(int questionId)
        {
            var options = await _optionRepository.GetOptionsByQuestionIdAsync(questionId);
            return options.ToList();
        }
    }
}
