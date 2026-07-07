using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using LMS.Core.Enums;
using LMS.Core.Exception;
using LMS.Core.Models;
using LMS.DAL.Interfaces;

namespace LMS.BLL.Services
{
    public class LectureProgressService : ILectureProgressService
    {
        private readonly ILectureProgressRepository _lectureProgressRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ILectureRepository _lectureRepository;
        private readonly ICourseSectionRepository _courseSectionRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICertificateService _certificateService;

        public LectureProgressService(
            ILectureProgressRepository lectureProgressRepository,
            IEnrollmentRepository enrollmentRepository,
            ILectureRepository lectureRepository,
            ICourseSectionRepository courseSectionRepository,
            ICourseRepository courseRepository,
            IUserRepository userRepository,
            ICertificateService certificateService)
        {
            _lectureProgressRepository = lectureProgressRepository;
            _enrollmentRepository = enrollmentRepository;
            _lectureRepository = lectureRepository;
            _courseSectionRepository = courseSectionRepository;
            _courseRepository = courseRepository;
            _userRepository = userRepository;
            _certificateService = certificateService;
        }

        public async Task<bool> UpdateProgressAsync(Guid userGuid, ProgressUpdateRequest request)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var lecture = await _lectureRepository.Get(request.LectureId);
            if (lecture == null)
                throw new NotFoundException(nameof(Lecture), request.LectureId);

            var section = await _courseSectionRepository.Get(lecture.CourseSectionId);
            if (section == null)
                throw new NotFoundException(nameof(CourseSection), lecture.CourseSectionId);

            var courseId = section.CourseId;
            var enrollments = await _enrollmentRepository.GetEnrollmentsByUserIdAsync(user.Id);
            var enrollment = enrollments.FirstOrDefault(e => e.CourseId == courseId);
            if (enrollment == null)
                throw new InvalidOperationException("User is not enrolled in the course associated with this lecture.");

            // Prevent un-completing a lecture if the user has already earned a certificate for this course
            if (!request.IsCompleted)
            {
                var associatedCourse = await _courseRepository.Get(courseId);
                if (associatedCourse != null)
                {
                    var existingCertificate = await _certificateService.GetCertificateAsync(associatedCourse.ExternalId, userGuid);
                    if (existingCertificate != null)
                    {
                        return false; // Silently reject the un-complete request
                    }
                }
            }

            var progress = await _lectureProgressRepository.GetProgressByLectureAndEnrollmentAsync(request.LectureId, enrollment.Id);
            var newStatus = request.IsCompleted ? LectureStatus.Completed : LectureStatus.InProgress;

            if (progress != null)
            {
                progress.Status = newStatus;
                progress.WatchedSeconds = request.WatchedSeconds;
                progress.LastAccessedAt = DateTime.UtcNow;
                await _lectureProgressRepository.Update(progress);
            }
            else
            {
                progress = new LectureProgress
                {
                    LectureId = request.LectureId,
                    EnrollmentId = enrollment.Id,
                    Status = newStatus,
                    WatchedSeconds = request.WatchedSeconds,
                    LastAccessedAt = DateTime.UtcNow
                };
                await _lectureProgressRepository.Create(progress);
            }

            // Check if course progress is 100% and generate certificate
            var course = await _courseRepository.GetCourseWithDetailsAsync(courseId);
            if (course != null)
            {
                int totalLectures = course.Sections.SelectMany(s => s.Lectures).Count();
                if (totalLectures > 0)
                {
                    var progresses = await _lectureProgressRepository.GetProgressByEnrollmentIdAsync(enrollment.Id);
                    int completedCount = progresses.Count(p => p.Status == LectureStatus.Completed);

                    if (completedCount == totalLectures)
                    {
                        // Generate certificate
                        await _certificateService.GenerateCertificateAsync(course.ExternalId, userGuid);
                    }
                }
            }

            return true;
        }

        public async Task<ProgressResponse> GetProgressAsync(Guid userGuid, int enrollmentId)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var enrollment = await _enrollmentRepository.GetEnrollmentWithDetailsAsync(enrollmentId);
            if (enrollment == null)
                throw new NotFoundException(nameof(Enrollment), enrollmentId);

            if (enrollment.UserId != user.Id)
                throw new UnauthorizedAccessException("You are not authorized to view progress for this enrollment.");

            var course = await _courseRepository.GetCourseWithDetailsAsync(enrollment.CourseId);
            if (course == null)
                throw new NotFoundException(nameof(Course), enrollment.CourseId);

            int totalLectures = course.Sections.SelectMany(s => s.Lectures).Count();
            if (totalLectures == 0)
            {
                return new ProgressResponse
                {
                    CompletedLectures = 0,
                    TotalLectures = 0,
                    Percentage = 0,
                    CompletedLectureIds = new List<int>()
                };
            }

            var progresses = await _lectureProgressRepository.GetProgressByEnrollmentIdAsync(enrollment.Id);
            var completedList = progresses.Where(p => p.Status == LectureStatus.Completed).Select(p => p.LectureId).ToList();

            return new ProgressResponse
            {
                CompletedLectures = completedList.Count,
                TotalLectures = totalLectures,
                Percentage = Math.Round(((double)completedList.Count / totalLectures) * 100.0, 2),
                CompletedLectureIds = completedList
            };
        }

        public async Task<ProgressResponse> GetProgressByCourseAsync(Guid userGuid, Guid courseGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null)
                throw new NotFoundException(nameof(Course), courseGuid);

            var enrollments = await _enrollmentRepository.GetEnrollmentsByUserIdAsync(user.Id);
            var enrollment = enrollments.FirstOrDefault(e => e.CourseId == course.Id);
            if (enrollment == null)
                throw new NotFoundException(nameof(Enrollment), $"Course GUID: {courseGuid}");

            return await GetProgressAsync(userGuid, enrollment.Id);
        }
    }
}
