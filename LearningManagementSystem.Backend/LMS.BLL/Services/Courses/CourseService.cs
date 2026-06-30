using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using LMS.Core.Enums;
using LMS.Core.Models;
using LMS.Core.Exception;
using LMS.DAL.Interfaces;
using LMS.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.BLL.Services
{
    public class CourseService : ICourseService
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILanguageRepository _languageRepository;
        private readonly LMSDBContext _context;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;

        public CourseService(
            ICourseRepository courseRepository,
            IUserRepository userRepository,
            ICategoryRepository categoryRepository,
            ILanguageRepository languageRepository,
            LMSDBContext context,
            IMapper mapper,
            INotificationService notificationService)
        {
            _courseRepository = courseRepository;
            _userRepository = userRepository;
            _categoryRepository = categoryRepository;
            _languageRepository = languageRepository;
            _context = context;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        public async Task<(IEnumerable<CourseResponse> Items, int TotalCount)> GetCoursesAsync(
            int page, 
            int pageSize, 
            int? categoryId, 
            string? search, 
            decimal? minPrice, 
            decimal? maxPrice, 
            string? language, 
            string? sortBy)
        {
            var courses = await _courseRepository.GetCoursesWithDetailsAsync();

            // Filter by Published status
            var query = courses.Where(c => c.Status == CourseStatus.Published);

            if (categoryId.HasValue)
            {
                query = query.Where(c => c.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Title.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                                         c.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(c => c.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(c => c.Price <= maxPrice.Value);
            }

            if (!string.IsNullOrEmpty(language))
            {
                query = query.Where(c => c.Language != null && c.Language.Name.Equals(language, StringComparison.OrdinalIgnoreCase));
            }

            // Sorting
            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy.ToLower().Trim())
                {
                    case "price-asc":
                        query = query.OrderBy(c => c.Price);
                        break;
                    case "price-desc":
                        query = query.OrderByDescending(c => c.Price);
                        break;
                    case "rating":
                        query = query.OrderByDescending(c => c.CourseReviews?.Any() == true ? c.CourseReviews.Average(r => r.Rating) : 0.0);
                        break;
                    case "students":
                        query = query.OrderByDescending(c => c.Enrollments?.Count() ?? 0);
                        break;
                    case "newest":
                    default:
                        query = query.OrderByDescending(c => c.CreatedAt);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(c => c.CreatedAt);
            }

            var totalCount = query.Count();
            var items = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => {
                    var resp = _mapper.Map<CourseResponse>(c);
                    if (c.Language != null) resp.Language = c.Language.Name;
                    resp.StudentsCount = c.Enrollments?.Count() ?? 0;
                    resp.Rating = c.CourseReviews?.Any() == true ? c.CourseReviews.Average(r => r.Rating) : 0.0;
                    resp.Status = c.Status.ToString();
                    
                    // Clear lectures for bulk listing response
                    foreach (var section in resp.Sections)
                    {
                        section.Lectures = new List<LectureResponse>();
                    }
                    return resp;
                })
                .ToList();

            return (items, totalCount);
        }

        public async Task<CourseResponse?> GetCourseByIdAsync(Guid courseGuid, Guid? userGuid = null)
        {
            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null) return null;

            // Restrict access for non-published courses (Draft, PendingReview, Rejected, Archived)
            if (course.Status != CourseStatus.Published)
            {
                if (!userGuid.HasValue || userGuid.Value == Guid.Empty)
                    return null;

                var user = await _userRepository.Get(userGuid.Value);
                if (user == null) return null;

                bool isAdmin = user.Role?.Name?.Equals("Admin", StringComparison.OrdinalIgnoreCase) ?? false;
                bool isInstructor = course.InstructorId == user.Id;
                bool isEnrolled = course.Enrollments != null &&
                                  course.Enrollments.Any(e => e.UserId == user.Id &&
                                      (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed));

                // Admins, owning Instructors, and Enrolled students can access the course details.
                if (!isAdmin && !isInstructor && !isEnrolled)
                {
                    return null;
                }
            }

            var resp = _mapper.Map<CourseResponse>(course);
            if (course.Language != null) resp.Language = course.Language.Name;
            resp.StudentsCount = course.Enrollments?.Count() ?? 0;
            resp.Rating = course.CourseReviews?.Any() == true ? course.CourseReviews.Average(r => r.Rating) : 0.0;
            resp.Status = course.Status.ToString();

            // Authorization check: only Admin, course Instructor, or Enrolled students can see the lectures list
            bool showLectures = false;
            if (userGuid.HasValue && userGuid.Value != Guid.Empty)
            {
                var user = await _userRepository.Get(userGuid.Value);
                if (user != null)
                {
                    bool isAdmin = user.Role?.Name?.Equals("Admin", StringComparison.OrdinalIgnoreCase) ?? false;
                    bool isInstructor = course.InstructorId == user.Id;
                    bool isEnrolled = course.Enrollments != null &&
                                      course.Enrollments.Any(e => e.UserId == user.Id &&
                                          (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed));

                    showLectures = isAdmin || isInstructor || isEnrolled;
                }
            }

            if (course.OriginalCourseId.HasValue)
            {
                var originalCourse = await _courseRepository.GetCourseWithDetailsAsync(course.OriginalCourseId.Value);
                if (originalCourse != null)
                {
                    resp.OriginalCourseExternalId = originalCourse.ExternalId;
                    resp.OriginalCourseDetails = _mapper.Map<CourseResponse>(originalCourse);
                }
            }

            if (!showLectures)
            {
                foreach (var section in resp.Sections)
                {
                    section.Lectures = new List<LectureResponse>();
                }
            }

            return resp;
        }

        public async Task<CourseResponse> CreateCourseAsync(CourseCreateRequest request, Guid instructorGuid)
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var instructor = await _userRepository.Get(instructorGuid);
                if (instructor == null)
                {
                    throw new NotFoundException("User", instructorGuid);
                }

                if (string.IsNullOrWhiteSpace(request.Title))
                {
                    throw new ArgumentException("Course title cannot be empty.");
                }

                var allCourses = await _courseRepository.GetAllAsync() ?? Enumerable.Empty<Course>();
                var hasDuplicateTitle = allCourses.Any(c => c.InstructorId == instructor.Id && 
                                                            c.Title.Trim().Equals(request.Title.Trim(), StringComparison.OrdinalIgnoreCase));
                if (hasDuplicateTitle)
                {
                    throw new ArgumentException("A course with this title already exists for this instructor.");
                }

                var category = await _categoryRepository.Get(request.CategoryId);
                if (category == null)
                {
                    throw new NotFoundException("Category", request.CategoryId);
                }

                // Look up or create language
                var allLanguages = await _languageRepository.GetAllAsync();
                var language = allLanguages.FirstOrDefault(l => l.Name.Equals(request.Language, StringComparison.OrdinalIgnoreCase));

                if (language == null)
                {
                    language = new Language { Name = request.Language };
                    language = await _languageRepository.Create(language);
                }

                var course = new Course
                {
                    Title = request.Title,
                    Description = request.Description,
                    CategoryId = request.CategoryId,
                    LanguageId = language.Id,
                    Price = request.Price,
                    ThumbnailUrl = request.ThumbnailUrl,
                    InstructorId = instructor.Id,
                    Status = CourseStatus.Draft,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdCourse = await _courseRepository.Create(course);
                var resp = _mapper.Map<CourseResponse>(createdCourse);
                resp.Language = language.Name;
                resp.Status = createdCourse.Status.ToString();

                await dbTransaction.CommitAsync();
                return resp;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<CourseUpdateResult> UpdateCourseAsync(Guid courseGuid, CourseUpdateRequest request, Guid userGuid)
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _userRepository.Get(userGuid);
                if (user == null)
                    throw new NotFoundException(nameof(User), userGuid);

                var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
                if (course == null) return new CourseUpdateResult { Success = false };

                // Admins and other users cannot edit courses. Only the owning Instructor can.
                if (course.InstructorId != user.Id)
                {
                    throw new UnauthorizedAccessException("Only the course instructor is authorized to update it.");
                }

                var category = await _categoryRepository.Get(request.CategoryId);
                if (category == null)
                {
                    throw new NotFoundException("Category", request.CategoryId);
                }

                // Look up or create language
                var allLanguages = await _languageRepository.GetAllAsync();
                var language = allLanguages.FirstOrDefault(l => l.Name.Equals(request.Language, StringComparison.OrdinalIgnoreCase));

                if (language == null)
                {
                    language = new Language { Name = request.Language };
                    language = await _languageRepository.Create(language);
                }

                Course targetCourse = course;

                // If editing a Published course, we duplicate it into a Draft first (unless a draft already exists)
                if (course.Status == CourseStatus.Published)
                {
                    var existingDraft = await _courseRepository.GetDraftByOriginalCourseIdAsync(course.Id);
                    if (existingDraft != null)
                    {
                        targetCourse = existingDraft;
                    }
                    else
                    {
                        targetCourse = await DuplicateCourseToDraftAsync(course);
                    }
                }

                targetCourse.Title = request.Title;
                targetCourse.Description = request.Description;
                targetCourse.CategoryId = request.CategoryId;
                targetCourse.LanguageId = language.Id;
                targetCourse.Price = request.Price;
                targetCourse.ThumbnailUrl = request.ThumbnailUrl;
                targetCourse.UpdatedAt = DateTime.UtcNow;
                targetCourse.Status = CourseStatus.Draft;

                await _courseRepository.Update(targetCourse);
                await dbTransaction.CommitAsync();

                return new CourseUpdateResult { Success = true, UpdatedCourseGuid = targetCourse.ExternalId };
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteCourseAsync(Guid courseGuid, Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null) return false;

            bool isAdmin = user.Role?.Name?.Equals("Admin", StringComparison.OrdinalIgnoreCase) ?? false;
            bool isInstructor = course.InstructorId == user.Id;

            if (!isAdmin && !isInstructor)
            {
                throw new UnauthorizedAccessException("You are not authorized to delete this course.");
            }

            bool hasActiveEnrollments = course.Enrollments != null &&
                                       course.Enrollments.Any(e => e.Status == EnrollmentStatus.Active);

            if (hasActiveEnrollments)
            {
                // Soft-delete if there are active learners: status becomes Archived
                course.Status = CourseStatus.Archived;
                course.UpdatedAt = DateTime.UtcNow;
                await _courseRepository.Update(course);
            }
            else
            {
                // Hard-delete if there are no active learners
                await _courseRepository.Delete(course);
            }

            return true;
        }

        public async Task<bool> SubmitForReviewAsync(Guid courseGuid, Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null) return false;

            if (course.InstructorId != user.Id)
            {
                throw new UnauthorizedAccessException("Only the course instructor is authorized to submit it for review.");
            }

            if (course.Status == CourseStatus.PendingReview)
            {
                throw new InvalidOperationException("The course is already pending review.");
            }
            if (course.Status == CourseStatus.Published)
            {
                throw new InvalidOperationException("The course is already published.");
            }

            course.Status = CourseStatus.PendingReview;
            course.UpdatedAt = DateTime.UtcNow;
            await _courseRepository.Update(course);
            return true;
        }

        public async Task<bool> PublishCourseAsync(Guid courseGuid)
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
                if (course == null) return false;

                var instructor = await _userRepository.GetUserWithRoleAsync(course.InstructorId);
                string recipient = instructor?.Email ?? "nandhiraja16@gmail.com";

                if (course.OriginalCourseId.HasValue)
                {
                    var original = await _courseRepository.GetCourseWithDetailsAsync(course.OriginalCourseId.Value);
                    if (original != null)
                    {
                        await MergeDraftToOriginalCourseAsync(course, original);
                    }
                }
                else
                {
                    course.Status = CourseStatus.Published;
                    course.UpdatedAt = DateTime.UtcNow;
                    await _courseRepository.Update(course);
                }

                await dbTransaction.CommitAsync();

                string emailBody = $@"
                    <h2>Course Approved</h2>
                    <p>We are pleased to inform you that your course '{course.Title}' has been approved and published on LMS.</p>
                    <p>Best regards,<br/>LMS Team</p>";
                await _notificationService.SendEmailAsync(recipient, "Course Approved", emailBody);

                return true;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> RejectCourseAsync(Guid courseGuid, string reason)
        {
            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null) return false;

            course.Status = CourseStatus.Rejected;
            course.UpdatedAt = DateTime.UtcNow;
            await _courseRepository.Update(course);

            var instructor = await _userRepository.GetUserWithRoleAsync(course.InstructorId);
            string recipient = instructor?.Email ?? "nandhiraja16@gmail.com";

            string emailBody = $@"
                <h2>Course Review Rejected</h2>
                <p>We regret to inform you that your course '{course.Title}' was rejected during our review process.</p>
                <p><strong>Reason:</strong> {reason}</p>
                <p>Please address this feedback and resubmit your course.</p>
                <p>Best regards,<br/>LMS Team</p>";
            await _notificationService.SendEmailAsync(recipient, "Course Review Rejected", emailBody);

            return true;
        }

        public async Task<bool> UploadThumbnailAsync(Guid courseGuid, string fileUrl, Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null) return false;

            if (course.InstructorId != user.Id)
            {
                throw new UnauthorizedAccessException("Only the course instructor is authorized to upload a thumbnail.");
            }

            course.ThumbnailUrl = fileUrl;
            course.UpdatedAt = DateTime.UtcNow;
            await _courseRepository.Update(course);
            return true;
        }

        #region Course Draft Helper Methods

        private async Task<Course> DuplicateCourseToDraftAsync(Course course)
        {
            var clonedCourse = new Course
            {
                Title = course.Title,
                Description = course.Description,
                CategoryId = course.CategoryId,
                LanguageId = course.LanguageId,
                Price = course.Price,
                ThumbnailUrl = course.ThumbnailUrl,
                DiscountPercentage = course.DiscountPercentage,
                InstructorId = course.InstructorId,
                Status = CourseStatus.Draft,
                OriginalCourseId = course.Id,
                ExternalId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Courses.Add(clonedCourse);
            await _context.SaveChangesAsync();

            var courseLevelQuizzes = _context.Quizzes
                .Where(q => q.CourseId == course.Id && q.LectureId == null)
                .ToList();

            foreach (var quiz in courseLevelQuizzes)
            {
                await DuplicateQuizAsync(quiz, clonedCourse.Id, null);
            }

            foreach (var section in course.Sections)
            {
                var clonedSection = new CourseSection
                {
                    Title = section.Title,
                    Description = section.Description ?? string.Empty,
                    Order = section.Order,
                    CourseId = clonedCourse.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.CourseSections.Add(clonedSection);
                await _context.SaveChangesAsync();

                foreach (var lecture in section.Lectures)
                {
                    var clonedLecture = new Lecture
                    {
                        Title = lecture.Title,
                        ContentUrl = lecture.ContentUrl,
                        ContentType = lecture.ContentType,
                        DurationInMinutes = lecture.DurationInMinutes,
                        Status = lecture.Status,
                        CourseSectionId = clonedSection.Id
                    };
                    _context.Lectures.Add(clonedLecture);
                    await _context.SaveChangesAsync();

                    foreach (var quiz in lecture.Quizzes)
                    {
                        await DuplicateQuizAsync(quiz, clonedCourse.Id, clonedLecture.Id);
                    }
                }
            }

            return clonedCourse;
        }

        private async Task DuplicateQuizAsync(Quiz quiz, int clonedCourseId, int? clonedLectureId)
        {
            var clonedQuiz = new Quiz
            {
                Title = quiz.Title,
                Description = quiz.Description ?? string.Empty,
                LectureId = clonedLectureId,
                CourseId = clonedCourseId,
                TotalMarks = quiz.TotalMarks,
                PassScore = quiz.PassScore,
                MaxAttempts = quiz.MaxAttempts,
                CurrentAttempt = quiz.CurrentAttempt,
                Status = quiz.Status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Quizzes.Add(clonedQuiz);
            await _context.SaveChangesAsync();

            var questions = _context.QuizQuestions
                .Include(q => q.Options)
                .Where(q => q.QuizId == quiz.Id)
                .ToList();

            foreach (var question in questions)
            {
                var clonedQuestion = new QuizQuestion
                {
                    QuizId = clonedQuiz.Id,
                    QuestionText = question.QuestionText
                };
                _context.QuizQuestions.Add(clonedQuestion);
                await _context.SaveChangesAsync();

                foreach (var option in question.Options)
                {
                    var clonedOption = new QuizOption
                    {
                        QuizQuestionId = clonedQuestion.Id,
                        OptionText = option.OptionText,
                        IsCorrect = option.IsCorrect
                    };
                    _context.QuizOptions.Add(clonedOption);
                }
                await _context.SaveChangesAsync();
            }
        }

        private async Task MergeDraftToOriginalCourseAsync(Course course, Course original)
        {
            original.Title = course.Title;
            original.Description = course.Description;
            original.CategoryId = course.CategoryId;
            original.LanguageId = course.LanguageId;
            original.Price = course.Price;
            original.ThumbnailUrl = course.ThumbnailUrl;
            original.DiscountPercentage = course.DiscountPercentage;
            original.UpdatedAt = DateTime.UtcNow;
            original.Status = CourseStatus.Published;

            var originalSections = original.Sections.ToList();
            var draftSections = course.Sections.ToList();

            foreach (var dSec in draftSections)
            {
                var oSec = originalSections.FirstOrDefault(s => s.Order == dSec.Order);
                if (oSec != null)
                {
                    oSec.Title = dSec.Title;
                    oSec.Description = dSec.Description;
                    oSec.UpdatedAt = DateTime.UtcNow;

                    await MergeLecturesAsync(dSec.Lectures.ToList(), oSec.Lectures.ToList(), oSec.Id, original.Id);
                }
                else
                {
                    await CreateNewSectionFromDraftAsync(dSec, original.Id);
                }
            }

            if (originalSections.Count > draftSections.Count)
            {
                var draftOrders = draftSections.Select(s => s.Order).ToHashSet();
                foreach (var oldSec in originalSections)
                {
                    if (!draftOrders.Contains(oldSec.Order))
                    {
                        var oldLectures = _context.Lectures.Where(l => l.CourseSectionId == oldSec.Id).ToList();
                        foreach (var ol in oldLectures)
                        {
                            var progresses = _context.LectureProgresses.Where(p => p.LectureId == ol.Id);
                            _context.LectureProgresses.RemoveRange(progresses);
                            _context.Lectures.Remove(ol);
                        }
                        _context.CourseSections.Remove(oldSec);
                    }
                }
            }

            await SyncQuizzesAsync(
                _context.Quizzes.Where(q => q.CourseId == course.Id && q.LectureId == null).ToList(),
                _context.Quizzes.Where(q => q.CourseId == original.Id && q.LectureId == null).ToList(),
                original.Id,
                null
            );

            var draftSectionsToDelete = _context.CourseSections.Where(s => s.CourseId == course.Id).ToList();
            foreach (var ds in draftSectionsToDelete)
            {
                var draftLecs = _context.Lectures.Where(l => l.CourseSectionId == ds.Id).ToList();
                foreach (var dl in draftLecs)
                {
                    var draftQuizzes = _context.Quizzes.Where(q => q.LectureId == dl.Id).ToList();
                    foreach (var dq in draftQuizzes)
                    {
                        var draftQuestions = _context.QuizQuestions.Where(q => q.QuizId == dq.Id).ToList();
                        foreach (var dqu in draftQuestions)
                        {
                            var opts = _context.QuizOptions.Where(o => o.QuizQuestionId == dqu.Id);
                            _context.QuizOptions.RemoveRange(opts);
                        }
                        _context.QuizQuestions.RemoveRange(draftQuestions);
                        _context.Quizzes.Remove(dq);
                    }
                    _context.Lectures.Remove(dl);
                }
                _context.CourseSections.Remove(ds);
            }
            _context.Courses.Remove(course);

            await _context.SaveChangesAsync();
            await _courseRepository.Update(original);
        }

        private async Task MergeLecturesAsync(List<Lecture> draftLectures, List<Lecture> originalLectures, int oSecId, int originalCourseId)
        {
            for (int i = 0; i < draftLectures.Count; i++)
            {
                var dLec = draftLectures[i];
                if (i < originalLectures.Count)
                {
                    var oLec = originalLectures[i];
                    oLec.Title = dLec.Title;
                    oLec.ContentUrl = dLec.ContentUrl;
                    oLec.ContentType = dLec.ContentType;
                    oLec.DurationInMinutes = dLec.DurationInMinutes;
                    oLec.Status = dLec.Status;

                    await SyncQuizzesAsync(dLec.Quizzes.ToList(), oLec.Quizzes.ToList(), originalCourseId, oLec.Id);
                }
                else
                {
                    await CreateNewLectureFromDraftAsync(dLec, oSecId, originalCourseId);
                }
            }

            if (originalLectures.Count > draftLectures.Count)
            {
                for (int i = draftLectures.Count; i < originalLectures.Count; i++)
                {
                    var oldLec = originalLectures[i];
                    var progresses = _context.LectureProgresses.Where(p => p.LectureId == oldLec.Id);
                    _context.LectureProgresses.RemoveRange(progresses);
                    _context.Lectures.Remove(oldLec);
                }
            }
        }

        private async Task SyncQuizzesAsync(List<Quiz> draftQuizzes, List<Quiz> originalQuizzes, int originalCourseId, int? oLecId)
        {
            for (int j = 0; j < draftQuizzes.Count; j++)
            {
                var dQuiz = draftQuizzes[j];
                if (j < originalQuizzes.Count)
                {
                    var oQuiz = originalQuizzes[j];
                    oQuiz.Title = dQuiz.Title;
                    oQuiz.Description = dQuiz.Description;
                    oQuiz.TotalMarks = dQuiz.TotalMarks;
                    oQuiz.PassScore = dQuiz.PassScore;
                    oQuiz.MaxAttempts = dQuiz.MaxAttempts;
                    oQuiz.CurrentAttempt = dQuiz.CurrentAttempt;
                    oQuiz.Status = dQuiz.Status;
                    oQuiz.UpdatedAt = DateTime.UtcNow;

                    var oldQuestions = _context.QuizQuestions.Where(q => q.QuizId == oQuiz.Id).ToList();
                    _context.QuizQuestions.RemoveRange(oldQuestions);

                    var draftQuestions = _context.QuizQuestions.Include(q => q.Options).Where(q => q.QuizId == dQuiz.Id).ToList();
                    foreach (var dq in draftQuestions)
                    {
                        var newQ = new QuizQuestion
                        {
                            QuizId = oQuiz.Id,
                            QuestionText = dq.QuestionText
                        };
                        _context.QuizQuestions.Add(newQ);
                        await _context.SaveChangesAsync();

                        foreach (var dOpt in dq.Options)
                        {
                            var newOpt = new QuizOption
                            {
                                QuizQuestionId = newQ.Id,
                                OptionText = dOpt.OptionText,
                                IsCorrect = dOpt.IsCorrect
                            };
                            _context.QuizOptions.Add(newOpt);
                        }
                    }
                }
                else
                {
                    var newQuiz = new Quiz
                    {
                        Title = dQuiz.Title,
                        Description = dQuiz.Description,
                        LectureId = oLecId,
                        CourseId = originalCourseId,
                        TotalMarks = dQuiz.TotalMarks,
                        PassScore = dQuiz.PassScore,
                        MaxAttempts = dQuiz.MaxAttempts,
                        CurrentAttempt = dQuiz.CurrentAttempt,
                        Status = dQuiz.Status,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Quizzes.Add(newQuiz);
                    await _context.SaveChangesAsync();

                    var draftQuestions = _context.QuizQuestions.Include(q => q.Options).Where(q => q.QuizId == dQuiz.Id).ToList();
                    foreach (var dq in draftQuestions)
                    {
                        var newQ = new QuizQuestion
                        {
                            QuizId = newQuiz.Id,
                            QuestionText = dq.QuestionText
                        };
                        _context.QuizQuestions.Add(newQ);
                        await _context.SaveChangesAsync();

                        foreach (var dOpt in dq.Options)
                        {
                            var newOpt = new QuizOption
                            {
                                QuizQuestionId = newQ.Id,
                                OptionText = dOpt.OptionText,
                                IsCorrect = dOpt.IsCorrect
                            };
                            _context.QuizOptions.Add(newOpt);
                        }
                    }
                }
            }

            if (originalQuizzes.Count > draftQuizzes.Count)
            {
                for (int j = draftQuizzes.Count; j < originalQuizzes.Count; j++)
                {
                    _context.Quizzes.Remove(originalQuizzes[j]);
                }
            }
        }

        private async Task CreateNewSectionFromDraftAsync(CourseSection dSec, int originalCourseId)
        {
            var newSec = new CourseSection
            {
                Title = dSec.Title,
                Description = dSec.Description ?? string.Empty,
                Order = dSec.Order,
                CourseId = originalCourseId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.CourseSections.Add(newSec);
            await _context.SaveChangesAsync();

            foreach (var dLec in dSec.Lectures)
            {
                await CreateNewLectureFromDraftAsync(dLec, newSec.Id, originalCourseId);
            }
        }

        private async Task CreateNewLectureFromDraftAsync(Lecture dLec, int originalSectionId, int originalCourseId)
        {
            var newLec = new Lecture
            {
                Title = dLec.Title,
                ContentUrl = dLec.ContentUrl,
                ContentType = dLec.ContentType,
                DurationInMinutes = dLec.DurationInMinutes,
                Status = dLec.Status,
                CourseSectionId = originalSectionId
            };
            _context.Lectures.Add(newLec);
            await _context.SaveChangesAsync();

            foreach (var dQuiz in dLec.Quizzes)
            {
                var newQuiz = new Quiz
                {
                    Title = dQuiz.Title,
                    Description = dQuiz.Description,
                    LectureId = newLec.Id,
                    CourseId = originalCourseId,
                    TotalMarks = dQuiz.TotalMarks,
                    PassScore = dQuiz.PassScore,
                    MaxAttempts = dQuiz.MaxAttempts,
                    CurrentAttempt = dQuiz.CurrentAttempt,
                    Status = dQuiz.Status,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Quizzes.Add(newQuiz);
                await _context.SaveChangesAsync();

                var draftQuestions = _context.QuizQuestions.Include(q => q.Options).Where(q => q.QuizId == dQuiz.Id).ToList();
                foreach (var dq in draftQuestions)
                {
                    var newQ = new QuizQuestion
                    {
                        QuizId = newQuiz.Id,
                        QuestionText = dq.QuestionText
                    };
                    _context.QuizQuestions.Add(newQ);
                    await _context.SaveChangesAsync();

                    foreach (var dOpt in dq.Options)
                    {
                        var newOpt = new QuizOption
                        {
                            QuizQuestionId = newQ.Id,
                            OptionText = dOpt.OptionText,
                            IsCorrect = dOpt.IsCorrect
                        };
                        _context.QuizOptions.Add(newOpt);
                    }
                }
            }
        }

        #endregion
    }
}
