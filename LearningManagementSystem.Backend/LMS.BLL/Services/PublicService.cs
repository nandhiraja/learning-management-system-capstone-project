using LMS.BLL.Interfaces;
using LMS.Core.DTOs.PublicDTOs;
using LMS.DAL.Interfaces;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Services
{
    public class PublicService : IPublicService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICourseRepository _courseRepository;

        public PublicService(IUserRepository userRepository, ICourseRepository courseRepository)
        {
            _userRepository = userRepository;
            _courseRepository = courseRepository;
        }

        public async Task<LandingStatsResponse> GetLandingStatsAsync()
        {
            var users = await _userRepository.GetAllUsersWithRolesAsync();
            var courses = await _courseRepository.GetAllAsync();

            return new LandingStatsResponse
            {
                TotalActiveStudents = users.Count(u => u.Role?.Name == "Student" && u.IsActive),
                TotalExpertInstructors = users.Count(u => u.Role?.Name == "Instructor" && u.IsActive),
                TotalPremiumCourses = courses.Count(c => c.Status == Core.Enums.CourseStatus.Published)
            };
        }

        public async Task<IEnumerable<TopInstructorResponse>> GetTopInstructorsAsync(int limit = 4)
        {
            // Fetch all users with Instructor role
            var allUsers = await _userRepository.GetAllUsersWithRolesAsync();
            var instructors = allUsers.Where(u => u.Role?.Name == "Instructor" && u.IsActive).ToList();

            // To get top instructors, we can sort by number of published courses and enrollments
            // For a basic implementation, let's sort by the number of published courses they have.
            var courses = await _courseRepository.GetCoursesWithDetailsAsync();
            var publishedCourses = courses.Where(c => c.Status == Core.Enums.CourseStatus.Published).ToList();

            var topInstructors = instructors
                .Select(inst => new
                {
                    Instructor = inst,
                    CourseCount = publishedCourses.Count(c => c.InstructorId == inst.Id),
                    TotalStudents = publishedCourses.Where(c => c.InstructorId == inst.Id).Sum(c => c.Enrollments?.Count() ?? 0)
                })
                .OrderByDescending(x => x.TotalStudents)
                .ThenByDescending(x => x.CourseCount)
                .Take(limit)
                .Select(x => new TopInstructorResponse
                {
                    Id = x.Instructor.ExternalId,
                    FullName = $"{x.Instructor.FirstName} {x.Instructor.LastName}".Trim(),
                    ProfilePictureUrl = x.Instructor.ProfilePictureUrl,
                    TotalStudents = x.TotalStudents,
                    CourseCount = x.CourseCount
                });

            return topInstructors;
        }
    }
}
