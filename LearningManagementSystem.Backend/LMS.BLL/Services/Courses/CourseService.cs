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

        public CourseService(
            ICourseRepository courseRepository,
            IUserRepository userRepository,
            ICategoryRepository categoryRepository,
            ILanguageRepository languageRepository,
            LMSDBContext context,
            IMapper mapper)
        {
            _courseRepository = courseRepository;
            _userRepository = userRepository;
            _categoryRepository = categoryRepository;
            _languageRepository = languageRepository;
            _context = context;
            _mapper = mapper;
        }

        public async Task<(IEnumerable<CourseResponse> Items, int TotalCount)> GetCoursesAsync(int page, int pageSize, int? categoryId, string? search)
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

                // If course is Archived, enrolled students can still view it. Otherwise only Admin and Instructor can.
                if (course.Status == CourseStatus.Archived)
                {
                    if (!isAdmin && !isInstructor && !isEnrolled)
                        return null;
                }
                else
                {
                    if (!isAdmin && !isInstructor)
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

        public async Task<bool> UpdateCourseAsync(Guid courseGuid, CourseUpdateRequest request, Guid userGuid)
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _userRepository.Get(userGuid);
                if (user == null)
                    throw new NotFoundException(nameof(User), userGuid);

                var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
                if (course == null) return false;

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

                course.Title = request.Title;
                course.Description = request.Description;
                course.CategoryId = request.CategoryId;
                course.LanguageId = language.Id;
                course.Price = request.Price;
                course.UpdatedAt = DateTime.UtcNow;

                // Any updates force the status back to Draft, requiring Admin approval again
                course.Status = CourseStatus.Draft;

                await _courseRepository.Update(course);
                await dbTransaction.CommitAsync();
                return true;
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
            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null) return false;

            course.Status = CourseStatus.Published;
            course.UpdatedAt = DateTime.UtcNow;
            await _courseRepository.Update(course);
            return true;
        }

        public async Task<bool> RejectCourseAsync(Guid courseGuid, string reason)
        {
            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null) return false;

            course.Status = CourseStatus.Rejected;
            course.UpdatedAt = DateTime.UtcNow;
            await _courseRepository.Update(course);
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
    }
}
