using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IInstructorService
    {
        Task<InstructorDashboardResponse> GetDashboardDataAsync(Guid instructorGuid);
        Task<IEnumerable<InstructorCourseResponse>> GetCoursesAsync(Guid instructorGuid);
        Task<IEnumerable<InstructorStudentResponse>> GetCourseStudentsAsync(Guid instructorGuid, int courseId);
    }

    public class InstructorDashboardResponse
    {
        public int TotalCourses { get; set; }
        public int TotalStudents { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class InstructorCourseResponse
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = null!;
    }

    public class InstructorStudentResponse
    {
        public int UserId { get; set; }
        public string Name { get; set; } = null!;
    }
}
