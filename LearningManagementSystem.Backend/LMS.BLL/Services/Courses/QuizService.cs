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
using LMS.DAL.Data;

namespace LMS.BLL.Services
{
    public class QuizService : IQuizService
    {
        private readonly IQuizRepository _quizRepository;
        private readonly IQuizQuestionRepository _questionRepository;
        private readonly IQuizOptionRepository _optionRepository;
        private readonly ILectureRepository _lectureRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly LMSDBContext _context;
        private readonly IMapper _mapper;
        private readonly IRealTimeNotificationService _realTimeNotificationService;
        private readonly ICertificateService _certificateService;
        private readonly INotificationService _notificationService;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public QuizService(
            IQuizRepository quizRepository,
            IQuizQuestionRepository questionRepository,
            IQuizOptionRepository optionRepository,
            ILectureRepository lectureRepository,
            IUserRepository userRepository,
            IEnrollmentRepository enrollmentRepository,
            ICourseRepository courseRepository,
            LMSDBContext context,
            IMapper mapper,
            IRealTimeNotificationService realTimeNotificationService,
            ICertificateService certificateService,
            INotificationService notificationService,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _quizRepository = quizRepository;
            _questionRepository = questionRepository;
            _optionRepository = optionRepository;
            _lectureRepository = lectureRepository;
            _userRepository = userRepository;
            _enrollmentRepository = enrollmentRepository;
            _courseRepository = courseRepository;
            _context = context;
            _mapper = mapper;
            _realTimeNotificationService = realTimeNotificationService;
            _certificateService = certificateService;
            _notificationService = notificationService;
            _configuration = configuration;
        }

        public async Task<QuizResponse> CreateQuizAsync(int lectureId, QuizRequest request, Guid userGuid)
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _userRepository.Get(userGuid);
                if (user == null)
                    throw new NotFoundException(nameof(User), userGuid);

                var lecture = await _lectureRepository.GetLectureWithDetailsAsync(lectureId);
                if (lecture == null)
                {
                    throw new NotFoundException("Lecture", lectureId);
                }

                // Admins and other users cannot add quizzes. Only the owning Instructor can.
                if (lecture.CourseSection?.Course?.InstructorId != user.Id)
                {
                    throw new UnauthorizedAccessException("Only the course instructor is authorized to create quizzes for it.");
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

                await dbTransaction.CommitAsync();
                return resp;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
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

        public async Task<bool> UpdateQuizAsync(int quizId, QuizRequest request, Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var quiz = await _quizRepository.Get(quizId);
            if (quiz == null) return false;

            var course = await _courseRepository.Get(quiz.CourseId);
            if (course == null) return false;

            // Admins and other users cannot edit quizzes. Only the owning Instructor can.
            if (course.InstructorId != user.Id)
            {
                throw new UnauthorizedAccessException("Only the course instructor is authorized to update quizzes in it.");
            }

            quiz.Title = request.Title;
            quiz.PassScore = request.PassScore;
            quiz.UpdatedAt = DateTime.UtcNow;

            await _quizRepository.Update(quiz);
            return true;
        }

        public async Task<bool> DeleteQuizAsync(int quizId, Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var quiz = await _quizRepository.Get(quizId);
            if (quiz == null) return false;

            var course = await _courseRepository.GetCourseWithDetailsAsync(quiz.CourseId);
            if (course == null) return false;

            // Only the owning Instructor can delete quizzes. Admins cannot edit/delete directly.
            if (course.InstructorId != user.Id)
            {
                throw new UnauthorizedAccessException("Only the course instructor is authorized to delete quizzes from it.");
            }

            // Cannot delete if there are active learners
            bool hasActiveEnrollments = course.Enrollments != null &&
                                       course.Enrollments.Any(e => e.Status == EnrollmentStatus.Active);

            if (hasActiveEnrollments)
            {
                throw new InvalidOperationException("Cannot delete course materials while there are active learners in the course.");
            }

            await _quizRepository.Delete(quiz);
            return true;
        }

        public async Task<QuizQuestionResponse> AddQuestionToQuizAsync(int quizId, QuizQuestionRequest request, Guid userGuid)
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _userRepository.Get(userGuid);
                if (user == null)
                    throw new NotFoundException(nameof(User), userGuid);

                var quiz = await _quizRepository.Get(quizId);
                if (quiz == null)
                {
                    throw new NotFoundException("Quiz", quizId);
                }

                var course = await _courseRepository.Get(quiz.CourseId);
                if (course == null)
                {
                    throw new NotFoundException("Course", quiz.CourseId);
                }

                // Admins and other users cannot edit quizzes. Only the owning Instructor can.
                if (course.InstructorId != user.Id)
                {
                    throw new UnauthorizedAccessException("Only the course instructor is authorized to manage quiz questions.");
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

                await dbTransaction.CommitAsync();
                return resp;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateQuestionAsync(int questionId, QuizQuestionRequest request, Guid userGuid)
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _userRepository.Get(userGuid);
                if (user == null)
                    throw new NotFoundException(nameof(User), userGuid);

                var question = await _questionRepository.Get(questionId);
                if (question == null) return false;

                var quiz = await _quizRepository.Get(question.QuizId);
                if (quiz == null) return false;

                var course = await _courseRepository.Get(quiz.CourseId);
                if (course == null) return false;

                // Admins and other users cannot edit quizzes. Only the owning Instructor can.
                if (course.InstructorId != user.Id)
                {
                    throw new UnauthorizedAccessException("Only the course instructor is authorized to manage quiz questions.");
                }

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

                await dbTransaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteQuestionAsync(int questionId, Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var question = await _questionRepository.Get(questionId);
            if (question == null) return false;

            var quiz = await _quizRepository.Get(question.QuizId);
            if (quiz == null) return false;

            var course = await _courseRepository.GetCourseWithDetailsAsync(quiz.CourseId);
            if (course == null) return false;

            // Only the owning Instructor can delete questions. Admins cannot.
            if (course.InstructorId != user.Id)
            {
                throw new UnauthorizedAccessException("Only the course instructor is authorized to manage quiz questions.");
            }

            // Cannot delete if there are active learners
            bool hasActiveEnrollments = course.Enrollments != null &&
                                       course.Enrollments.Any(e => e.Status == EnrollmentStatus.Active);

            if (hasActiveEnrollments)
            {
                throw new InvalidOperationException("Cannot delete course materials while there are active learners in the course.");
            }

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

            var course = await _courseRepository.Get(quiz.CourseId);

            // Verify authorization: only Enrolled students, course Instructor, or Admins can submit the quiz
            var enrollments = await _enrollmentRepository.GetEnrollmentsByUserIdAsync(user.Id);
            var enrollment = enrollments.FirstOrDefault(e => e.CourseId == quiz.CourseId);
            bool isEnrolled = enrollment != null && (enrollment.Status == EnrollmentStatus.Active || enrollment.Status == EnrollmentStatus.Completed);
            bool isInstructor = course != null && course.InstructorId == user.Id;
            bool isAdmin = user.Role?.Name?.Equals("Admin", StringComparison.OrdinalIgnoreCase) ?? false;

            if (!isAdmin && !isInstructor && !isEnrolled)
            {
                throw new UnauthorizedAccessException("You must be enrolled in the course to submit this quiz.");
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

            // Update QuizProgress
            var quizProgress = _context.QuizProgresses.FirstOrDefault(qp => qp.UserId == user.Id && qp.QuizId == quizId);
            if (quizProgress != null)
            {
                if (quizProgress.IsPassed || quizProgress.AttemptsUsed >= quiz.MaxAttempts)
                {
                    throw new InvalidOperationException("You cannot submit this quiz again.");
                }

                quizProgress.AttemptsUsed++;
                if (score > quizProgress.HighestScore)
                    quizProgress.HighestScore = score;
                
                if (passed)
                    quizProgress.IsPassed = true;
                
                quizProgress.LastAttemptDate = DateTime.UtcNow;
            }
            else
            {
                quizProgress = new QuizProgress
                {
                    UserId = user.Id,
                    QuizId = quizId,
                    AttemptsUsed = 1,
                    HighestScore = score,
                    IsPassed = passed,
                    LastAttemptDate = DateTime.UtcNow
                };
                _context.QuizProgresses.Add(quizProgress);
            }
            
            await _context.SaveChangesAsync();

            try
            {
                await _realTimeNotificationService.CreateAndSendNotificationAsync(user.Id, "Quiz Completed", $"You completed the quiz '{quiz.Title}' with a score of {score}%!", "Quiz");
            }
            catch (Exception)
            {
            }

            // Check if course progress is 100% and generate certificate
            if (passed && course != null)
            {
                var allLectures = _context.Lectures.Where(l => l.CourseSection.CourseId == quiz.CourseId).ToList();
                int totalLectures = allLectures.Count;

                var allQuizzes = _context.Quizzes.Where(q => q.CourseId == quiz.CourseId).ToList();
                int totalQuizzes = allQuizzes.Count;

                if (enrollment != null)
                {
                    var lectureProgresses = _context.LectureProgresses.Where(p => p.EnrollmentId == enrollment.Id).ToList();
                    int completedLecturesCount = lectureProgresses.Count(p => p.Status == LectureStatus.Completed);

                    var quizIds = allQuizzes.Select(q => q.Id).ToList();
                    int passedQuizzesCount = _context.QuizProgresses.Count(qp => qp.UserId == user.Id && quizIds.Contains(qp.QuizId) && qp.IsPassed);

                    if (completedLecturesCount == totalLectures && passedQuizzesCount == totalQuizzes)
                    {
                        // Check if certificate already exists to avoid generating duplicate
                        var existingCert = _context.Certificates.FirstOrDefault(c => c.EnrollmentId == enrollment.Id);
                        if (existingCert == null)
                        {
                            var certificate = await _certificateService.GenerateCertificateAsync(course.ExternalId, userGuid);
                            
                            // Send course completion email
                            string emailBody = $@"
                                <h2>Course Completed! 🎉</h2>
                                <p>Congratulations, <strong>{user.FirstName} {user.LastName}</strong>!</p>
                                <p>You have successfully completed the course: <strong>{course.Title}</strong>.</p>
                                <div style='background-color: #f8fafc; padding: 24px; border-radius: 8px; margin: 20px 0;'>
                                    <p style='margin: 0; color: #475569;'>Certificate ID: <strong>{certificate.Id}</strong></p>
                                    <p style='margin: 8px 0 0 0; color: #475569;'>Issued: <strong>{certificate.IssuedDate:MMMM dd, yyyy}</strong></p>
                                </div>
                                <a href='{_configuration["FrontendBaseUrl"] ?? "http://localhost:4200"}/learning/dashboard' class='button' style='display: inline-block; padding: 12px 24px; background-color: #4f46e5; color: white; text-decoration: none; border-radius: 6px; font-weight: 500; margin-top: 20px;'>View Certificate</a>
                                <br/><br/>
                                <p>Keep up the great work!</p>";
                            await _notificationService.SendEmailAsync(user.Email, $"Congratulations! You've completed {course.Title}", emailBody);
                        }
                    }
                }
            }

            return new QuizSubmitResponse
            {
                Score = score,
                Passed = passed
            };
        }

        public async Task<QuizProgressResponse> GetQuizProgressAsync(int quizId, Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException("User", userGuid);

            var quiz = await _quizRepository.Get(quizId);
            if (quiz == null)
                throw new NotFoundException("Quiz", quizId);

            var quizProgress = _context.QuizProgresses.FirstOrDefault(qp => qp.UserId == user.Id && qp.QuizId == quizId);

            return new QuizProgressResponse
            {
                QuizId = quizId,
                AttemptsUsed = quizProgress?.AttemptsUsed ?? 0,
                MaxAttempts = quiz.MaxAttempts,
                HighestScore = quizProgress?.HighestScore ?? 0,
                PassScore = quiz.PassScore,
                IsPassed = quizProgress?.IsPassed ?? false
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
