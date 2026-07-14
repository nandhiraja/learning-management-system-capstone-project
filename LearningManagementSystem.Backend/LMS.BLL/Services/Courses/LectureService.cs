using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using LMS.Core.Enums;
using LMS.Core.Models;
using LMS.Core.Exception;
using LMS.DAL.Data;
using LMS.DAL.Interfaces;

namespace LMS.BLL.Services
{
    public class LectureService : ILectureService
    {
        private readonly ILectureRepository _lectureRepository;
        private readonly ICourseSectionRepository _sectionRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IAiServiceClient _aiServiceClient;
        private readonly LMSDBContext _context;

        public LectureService(
            ILectureRepository lectureRepository,
            ICourseSectionRepository sectionRepository,
            ICourseRepository courseRepository,
            IUserRepository userRepository,
            IMapper mapper,
            IAiServiceClient aiServiceClient,
            LMSDBContext context)
        {
            _lectureRepository = lectureRepository;
            _sectionRepository = sectionRepository;
            _courseRepository = courseRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _aiServiceClient = aiServiceClient;
            _context = context;
        }

        public async Task<LectureResponse> CreateLectureAsync(int sectionId, LectureRequest request, Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var section = await _sectionRepository.Get(sectionId);
            if (section == null)
            {
                throw new NotFoundException("CourseSection", sectionId);
            }

            var course = await _courseRepository.Get(section.CourseId);
            if (course == null)
            {
                throw new NotFoundException("Course", section.CourseId);
            }

            // Only the course's Instructor can create lectures. Admins cannot edit.
            if (course.InstructorId != user.Id)
            {
                throw new UnauthorizedAccessException("Only the course instructor is authorized to add lectures to it.");
            }

            if (!Enum.TryParse<ContentType>(request.ContentType, true, out var contentType))
            {
                throw new ArgumentException($"Invalid ContentType: {request.ContentType}");
            }

            var lecture = new Lecture
            {
                Title = request.Title,
                ContentUrl = request.ContentUrl,
                DurationInMinutes = request.DurationInMinutes,
                ContentType = contentType,
                CourseSectionId = sectionId,
                Status = LectureStatus.NotStarted
            };

            var createdLecture = await _lectureRepository.Create(lecture);

            if (course.Status == CourseStatus.Published)
            {
                course.Status = CourseStatus.Draft;
                await _courseRepository.Update(course);
            }

            var resp = _mapper.Map<LectureResponse>(createdLecture);
            resp.ContentType = createdLecture.ContentType.ToString();
            return resp;
        }

        public async Task<LectureResponse?> GetLectureByIdAsync(int lectureId, Guid userGuid)
        {
            var lecture = await _lectureRepository.GetLectureWithDetailsAsync(lectureId);
            if (lecture == null) return null;

            var user = await _userRepository.Get(userGuid);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            bool isAdmin = user.Role?.Name?.Equals("Admin", StringComparison.OrdinalIgnoreCase) ?? false;
            bool isInstructor = lecture.CourseSection?.Course?.InstructorId == user.Id;
            bool isEnrolled = lecture.CourseSection?.Course?.Enrollments != null &&
                              lecture.CourseSection.Course.Enrollments.Any(e => e.UserId == user.Id && 
                                  (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed));

            if (!isAdmin && !isInstructor && !isEnrolled)
            {
                throw new UnauthorizedAccessException("You do not have permission to access this lecture.");
            }

            var resp = _mapper.Map<LectureResponse>(lecture);
            resp.ContentUrl = lecture.ContentUrl;
            resp.ContentType = lecture.ContentType.ToString();
            return resp;
        }

        public async Task<bool> UpdateLectureAsync(int lectureId, LectureRequest request, Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var lecture = await _lectureRepository.GetLectureWithDetailsAsync(lectureId);
            if (lecture == null)
                throw new NotFoundException(nameof(Lecture), lectureId);

            // Only the course's Instructor can edit/update lectures. Admins cannot edit.
            if (lecture.CourseSection?.Course?.InstructorId != user.Id)
            {
                throw new UnauthorizedAccessException("Only the course instructor is authorized to update lectures in it.");
            }

            if (!Enum.TryParse<ContentType>(request.ContentType, true, out var contentType))
            {
                throw new ArgumentException($"Invalid ContentType: {request.ContentType}");
            }

            lecture.Title = request.Title;
            lecture.ContentUrl = request.ContentUrl;
            lecture.DurationInMinutes = request.DurationInMinutes;
            lecture.ContentType = contentType;

            await _lectureRepository.Update(lecture);

            var course = lecture.CourseSection?.Course;
            if (course != null && course.Status == CourseStatus.Published)
            {
                course.Status = CourseStatus.Draft;
                await _courseRepository.Update(course);
            }

            return true;
        }

        public async Task<bool> DeleteLectureAsync(int lectureId, Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var lecture = await _lectureRepository.GetLectureWithDetailsAsync(lectureId);
            if (lecture == null) return false;

            // Only the owning Instructor can delete lectures. Admins cannot edit/delete directly.
            if (lecture.CourseSection?.Course?.InstructorId != user.Id)
            {
                throw new UnauthorizedAccessException("Only the course instructor is authorized to delete lectures from it.");
            }

            // Cannot delete if there are active learners
            bool hasActiveEnrollments = lecture.CourseSection?.Course?.Enrollments != null &&
                                       lecture.CourseSection.Course.Enrollments.Any(e => e.Status == EnrollmentStatus.Active);

            if (hasActiveEnrollments)
            {
                throw new InvalidOperationException("Cannot delete course materials while there are active learners in the course.");
            }

            await _lectureRepository.Delete(lecture);

            var course = lecture.CourseSection?.Course;
            if (course != null && course.Status == CourseStatus.Published)
            {
                course.Status = CourseStatus.Draft;
                await _courseRepository.Update(course);
            }

            return true;
        }

        public async Task<bool> UploadLectureContentAsync(int lectureId, string fileUrl, Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var lecture = await _lectureRepository.GetLectureWithDetailsAsync(lectureId);
            if (lecture == null) return false;

            // Only the course's Instructor can edit/update content. Admins cannot.
            if (lecture.CourseSection?.Course?.InstructorId != user.Id)
            {
                throw new UnauthorizedAccessException("Only the course instructor is authorized to upload lecture content.");
            }

            lecture.ContentUrl = fileUrl;
            await _lectureRepository.Update(lecture);

            // PDF Text Extraction Integration
            if (lecture.ContentType == ContentType.pdf)
            {
                await ProcessPdfTranscriptionAsync(lecture, fileUrl);
            }

            var course = lecture.CourseSection?.Course;
            if (course != null && course.Status == CourseStatus.Published)
            {
                course.Status = CourseStatus.Draft;
                await _courseRepository.Update(course);
            }

            return true;
        }

        private async Task ProcessPdfTranscriptionAsync(Lecture lecture, string fileUrl)
        {
            try
            {
                // Clear any existing transcripts for this lecture (scenarios of re-upload)
                var existing = _context.LectureTranscripts.Where(t => t.LectureId == lecture.Id);
                _context.LectureTranscripts.RemoveRange(existing);
                await _context.SaveChangesAsync();

                // Get local filesystem path
                var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var fullPath = Path.Combine(webRoot, fileUrl.TrimStart('/'));

                if (File.Exists(fullPath))
                {
                    // Call PDF extraction service
                    var pages = await _aiServiceClient.ExtractPdfTextAsync(fullPath);

                    if (pages != null && pages.Any())
                    {
                        foreach (var page in pages)
                        {
                            if (!string.IsNullOrWhiteSpace(page.Text))
                            {
                                _context.LectureTranscripts.Add(new LectureTranscript
                                {
                                    LectureId = lecture.Id,
                                    Text = $"[Page {page.PageNumber}] {page.Text}",
                                    StartTime = page.PageNumber, // Page number is saved as StartTime
                                    EndTime = page.PageNumber
                                });
                            }
                        }
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // absorb error so main flow does not break if AI service is temporarily down
                System.Diagnostics.Debug.WriteLine($"Error processing PDF extraction: {ex.Message}");
            }
        }
    }
}
