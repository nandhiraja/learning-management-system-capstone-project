using LMS.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IInstructorService
    {
        Task<InstructorDashboardResponse> GetDashboardDataAsync(Guid instructorGuid);
        Task<IEnumerable<CourseResponse>> GetCoursesAsync(Guid instructorGuid);
        Task<IEnumerable<InstructorStudentResponse>> GetCourseStudentsAsync(Guid instructorGuid, int courseId);
        Task<IEnumerable<InstructorDiscussionResponse>> GetInstructorDiscussionsAsync(Guid instructorGuid, bool? unansweredOnly = null);
    }

    public class InstructorDashboardResponse
    {
        public int TotalCourses { get; set; }
        public int TotalStudents { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageCourseRating { get; set; }
        public int TotalReviewsCount { get; set; }
        public int UnansweredDiscussionsCount { get; set; }
    }

    public class InstructorDiscussionResponse
    {
        public Guid DiscussionGuid { get; set; }
        public Guid CourseGuid { get; set; }
        public string CourseTitle { get; set; } = null!;
        public string? LectureTitle { get; set; }
        public string StudentName { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int RepliesCount { get; set; }
        public bool IsAnswered { get; set; }
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
