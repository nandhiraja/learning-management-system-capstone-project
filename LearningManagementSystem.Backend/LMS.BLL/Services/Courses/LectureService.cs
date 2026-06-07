using System;
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
    public class LectureService : ILectureService
    {
        private readonly ILectureRepository _lectureRepository;
        private readonly ICourseSectionRepository _sectionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public LectureService(
            ILectureRepository lectureRepository,
            ICourseSectionRepository sectionRepository,
            IUserRepository userRepository,
            IMapper mapper)
        {
            _lectureRepository = lectureRepository;
            _sectionRepository = sectionRepository;
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<LectureResponse> CreateLectureAsync(int sectionId, LectureRequest request)
        {
            var section = await _sectionRepository.Get(sectionId);
            if (section == null)
            {
                throw new NotFoundException("CourseSection", sectionId);
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

        public async Task<bool> UpdateLectureAsync(int lectureId, LectureRequest request)
        {
            var lecture = await _lectureRepository.Get(lectureId);
            if (lecture == null) return false;

            if (!Enum.TryParse<ContentType>(request.ContentType, true, out var contentType))
            {
                throw new ArgumentException($"Invalid ContentType: {request.ContentType}");
            }

            lecture.Title = request.Title;
            lecture.ContentUrl = request.ContentUrl;
            lecture.DurationInMinutes = request.DurationInMinutes;
            lecture.ContentType = contentType;

            await _lectureRepository.Update(lecture);
            return true;
        }

        public async Task<bool> DeleteLectureAsync(int lectureId)
        {
            var lecture = await _lectureRepository.Get(lectureId);
            if (lecture == null) return false;

            await _lectureRepository.Delete(lecture);
            return true;
        }

        public async Task<bool> UploadLectureContentAsync(int lectureId, string fileUrl)
        {
            var lecture = await _lectureRepository.Get(lectureId);
            if (lecture == null) return false;

            lecture.ContentUrl = fileUrl;
            await _lectureRepository.Update(lecture);
            return true;
        }
    }
}
