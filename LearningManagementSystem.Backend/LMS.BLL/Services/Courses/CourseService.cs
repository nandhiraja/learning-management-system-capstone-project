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

namespace LMS.BLL.Services
{
    public class CourseService : ICourseService
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILanguageRepository _languageRepository;
        private readonly IMapper _mapper;

        public CourseService(
            ICourseRepository courseRepository,
            IUserRepository userRepository,
            ICategoryRepository categoryRepository,
            ILanguageRepository languageRepository,
            IMapper mapper)
        {
            _courseRepository = courseRepository;
            _userRepository = userRepository;
            _categoryRepository = categoryRepository;
            _languageRepository = languageRepository;
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
                    resp.Rating = 5;   // yet to implement
                    resp.Status = c.Status.ToString();
                    return resp;
                })
                .ToList();

            return (items, totalCount);
        }

        public async Task<CourseResponse?> GetCourseByIdAsync(Guid courseGuid)
        {
            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null) return null;

            var resp = _mapper.Map<CourseResponse>(course);
            if (course.Language != null) resp.Language = course.Language.Name;
            resp.StudentsCount = course.Enrollments?.Count() ?? 0;
            resp.Rating = course.CourseReviews?.Any() == true ? course.CourseReviews.Average(r => r.Rating) : 0.0;
            resp.Status = course.Status.ToString();
            return resp;
        }

        public async Task<CourseResponse> CreateCourseAsync(CourseCreateRequest request, Guid instructorGuid)
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
            return resp;
        }

        public async Task<bool> UpdateCourseAsync(Guid courseGuid, CourseUpdateRequest request)
        {
            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null) return false;

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

            await _courseRepository.Update(course);
            return true;
        }

        public async Task<bool> DeleteCourseAsync(Guid courseGuid)
        {
            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null) return false;

            // Only Draft courses can be deleted as per business requirements
            if (course.Status != CourseStatus.Draft)
            {
                throw new InvalidOperationException("Only draft courses can be deleted.");
            }

            await _courseRepository.Delete(course);
            return true;
        }

        public async Task<bool> SubmitForReviewAsync(Guid courseGuid)
        {
            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null) return false;

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
            // In a real system we would log or send email with the reason
            return true;
        }

        public async Task<bool> UploadThumbnailAsync(Guid courseGuid, string fileUrl)
        {
            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null) return false;

            course.ThumbnailUrl = fileUrl;
            course.UpdatedAt = DateTime.UtcNow;
            await _courseRepository.Update(course);
            return true;
        }
    }
}
