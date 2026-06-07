using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using LMS.Core.Enums;
using LMS.Core.Exception;
using LMS.Core.Models;
using LMS.DAL.Interfaces;

namespace LMS.BLL.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly ILectureProgressRepository _lectureProgressRepository;
        private readonly IMapper _mapper;

        public EnrollmentService(
            IEnrollmentRepository enrollmentRepository,
            IUserRepository userRepository,
            ICourseRepository courseRepository,
            ILectureProgressRepository lectureProgressRepository,
            IMapper mapper)
        {
            _enrollmentRepository = enrollmentRepository;
            _userRepository = userRepository;
            _courseRepository = courseRepository;
            _lectureProgressRepository = lectureProgressRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<EnrollmentResponse>> GetUserCoursesAsync(Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var enrollments = await _enrollmentRepository.GetEnrollmentsByUserIdAsync(user.Id);
            var responseList = new List<EnrollmentResponse>();

            foreach (var enrollment in enrollments)
            {
                var response = _mapper.Map<EnrollmentResponse>(enrollment);
                response.Progress = await CalculateProgressAsync(enrollment);
                responseList.Add(response);
            }

            return responseList;
        }

        public async Task<EnrollmentResponse?> GetEnrollmentDetailsAsync(Guid userGuid, Guid courseGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var course = await _courseRepository.GetByExternalIdAsync(courseGuid);
            if (course == null)
                throw new NotFoundException(nameof(Course), courseGuid);

            var enrollments = await _enrollmentRepository.GetEnrollmentsByUserIdAsync(user.Id);
            var enrollment = enrollments.FirstOrDefault(e => e.CourseId == course.Id);
            if (enrollment == null) return null;

            var response = _mapper.Map<EnrollmentResponse>(enrollment);
            response.Progress = await CalculateProgressAsync(enrollment);
            return response;
        }

        private async Task<double> CalculateProgressAsync(Enrollment enrollment)
        {
            var course = await _courseRepository.GetCourseWithDetailsAsync(enrollment.CourseId);
            if (course == null) return 0;

            int totalLectures = course.Sections.SelectMany(s => s.Lectures).Count();
            if (totalLectures == 0) return 0;

            var progresses = await _lectureProgressRepository.GetProgressByEnrollmentIdAsync(enrollment.Id);
            int completedLectures = progresses.Count(p => p.Status == LectureStatus.Completed);

            return Math.Round(((double)completedLectures / totalLectures) * 100.0, 2);
        }
    }
}
