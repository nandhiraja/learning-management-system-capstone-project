using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LMS.BLL.Interfaces;
using LMS.Core.Enums;
using LMS.Core.Exception;
using LMS.Core.Models;
using LMS.DAL.Interfaces;

namespace LMS.BLL.Services
{
    public class InstructorService : IInstructorService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IOrderRepository _orderRepository;

        public InstructorService(
            IUserRepository userRepository,
            ICourseRepository courseRepository,
            IEnrollmentRepository enrollmentRepository,
            IOrderRepository orderRepository)
        {
            _userRepository = userRepository;
            _courseRepository = courseRepository;
            _enrollmentRepository = enrollmentRepository;
            _orderRepository = orderRepository;
        }

        public async Task<InstructorDashboardResponse> GetDashboardDataAsync(Guid instructorGuid)
        {
            var user = await _userRepository.Get(instructorGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), instructorGuid);

            var allCourses = await _courseRepository.GetCoursesWithDetailsAsync();
            var instructorCourses = allCourses.Where(c => c.InstructorId == user.Id).ToList();
            
            if (!instructorCourses.Any())
            {
                return new InstructorDashboardResponse
                {
                    TotalCourses = 0,
                    TotalStudents = 0,
                    TotalRevenue = 0
                };
            }

            var courseIds = instructorCourses.Select(c => c.Id).ToList();

            // Fetch instructor-related enrollments via DB query
            var instructorEnrollments = await _enrollmentRepository.GetEnrollmentsByCourseIdsAsync(courseIds);
            int totalStudents = instructorEnrollments.Select(e => e.UserId).Distinct().Count();

            // Calculate total revenue from completed orders via optimized DB query
            decimal totalRevenue = await _orderRepository.GetRevenueByCourseIdsAsync(courseIds);

            return new InstructorDashboardResponse
            {
                TotalCourses = instructorCourses.Count,
                TotalStudents = totalStudents,
                TotalRevenue = totalRevenue
            };
        }

        public async Task<IEnumerable<InstructorCourseResponse>> GetCoursesAsync(Guid instructorGuid)
        {
            var user = await _userRepository.Get(instructorGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), instructorGuid);

            var allCourses = await _courseRepository.GetCoursesWithDetailsAsync();
            var instructorCourses = allCourses.Where(c => c.InstructorId == user.Id);

            return instructorCourses.Select(c => new InstructorCourseResponse
            {
                CourseId = c.Id,
                CourseTitle = c.Title
            });
        }

        public async Task<IEnumerable<InstructorStudentResponse>> GetCourseStudentsAsync(Guid instructorGuid, int courseId)
        {
            var user = await _userRepository.Get(instructorGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), instructorGuid);

            var course = await _courseRepository.Get(courseId);
            if (course == null)
                throw new NotFoundException(nameof(Course), courseId);

            if (course.InstructorId != user.Id)
                throw new UnauthorizedAccessException("You are not authorized to view student list for this course.");

            var enrollments = await _enrollmentRepository.GetEnrollmentsByCourseIdWithUserAsync(courseId);

            return enrollments.Select(e => new InstructorStudentResponse
            {
                UserId = e.UserId,
                Name = e.User != null ? $"{e.User.FirstName} {e.User.LastName}".Trim() : string.Empty
            });
        }
    }
}
