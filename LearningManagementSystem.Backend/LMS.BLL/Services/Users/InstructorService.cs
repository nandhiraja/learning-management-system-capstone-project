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
    public class InstructorService : IInstructorService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ICourseReviewRepository _reviewRepository;
        private readonly IDiscussionRepository _discussionRepository;
        private readonly IMapper _mapper;

        public InstructorService(
            IUserRepository userRepository,
            ICourseRepository courseRepository,
            IEnrollmentRepository enrollmentRepository,
            IOrderRepository orderRepository,
            ICourseReviewRepository reviewRepository,
            IDiscussionRepository discussionRepository,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _courseRepository = courseRepository;
            _enrollmentRepository = enrollmentRepository;
            _orderRepository = orderRepository;
            _reviewRepository = reviewRepository;
            _discussionRepository = discussionRepository;
            _mapper = mapper;
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
                    TotalRevenue = 0,
                    AverageCourseRating = 0.0,
                    TotalReviewsCount = 0,
                    UnansweredDiscussionsCount = 0
                };
            }

            var courseIds = instructorCourses.Select(c => c.Id).ToList();

            // Fetch instructor-related enrollments via DB query
            var instructorEnrollments = await _enrollmentRepository.GetEnrollmentsByCourseIdsAsync(courseIds);
            int totalStudents = instructorEnrollments.Select(e => e.UserId).Distinct().Count();

            // Calculate total revenue from completed orders via optimized DB query
            decimal totalRevenue = await _orderRepository.GetRevenueByCourseIdsAsync(courseIds);

            // Fetch reviews and calculate average rating and count
            var reviews = await _reviewRepository.GetReviewsByCourseIdsAsync(courseIds);
            double averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0.0;
            int totalReviews = reviews.Count();

            // Fetch discussions and count unanswered ones
            var discussions = await _discussionRepository.GetDiscussionsByCourseIdsAsync(courseIds);
            int unansweredCount = discussions.Count(d => !d.Replies.Any(r => r.UserId == user.Id));

            // 1. Enrollment Trends Chart (Last 6 months)
            var enrollmentTrendsChart = new ChartDataDto();
            for (int i = 5; i >= 0; i--)
            {
                var month = DateTime.Now.AddMonths(-i);
                enrollmentTrendsChart.Labels.Add(month.ToString("MMM yyyy"));
                enrollmentTrendsChart.Data.Add(instructorEnrollments.Count(e => e.EnrolledAt.Year == month.Year && e.EnrolledAt.Month == month.Month));
            }

            // 2. Revenue Per Course Chart
            var revenuePerCourseChart = new ChartDataDto();
            var allCompletedOrderItems = await _orderRepository.GetAllCompletedOrderItemsAsync();
            var instructorOrderItems = allCompletedOrderItems.Where(oi => courseIds.Contains(oi.CourseId)).ToList();
            
            var courseRevenue = new Dictionary<string, decimal>();
            foreach (var orderItem in instructorOrderItems)
            {
                var course = instructorCourses.FirstOrDefault(c => c.Id == orderItem.CourseId);
                if (course != null)
                {
                    if (!courseRevenue.ContainsKey(course.Title))
                        courseRevenue[course.Title] = 0;
                    courseRevenue[course.Title] += orderItem.FinalPrice;
                }
            }

            foreach (var kvp in courseRevenue.OrderByDescending(k => k.Value))
            {
                revenuePerCourseChart.Labels.Add(kvp.Key);
                revenuePerCourseChart.Data.Add(kvp.Value);
            }

            // 3. Ratings Distribution Chart
            var ratingsDistributionChart = new ChartDataDto();
            var ratingGroups = reviews.GroupBy(r => r.Rating).OrderBy(g => g.Key);
            foreach (var group in ratingGroups)
            {
                ratingsDistributionChart.Labels.Add($"{group.Key} Star");
                ratingsDistributionChart.Data.Add(group.Count());
            }

            // 4. Monthly Revenue Chart (Earnings per month for last 6 months)
            var monthlyRevenueChart = new ChartDataDto();
            for (int i = 5; i >= 0; i--)
            {
                var month = DateTime.Now.AddMonths(-i);
                monthlyRevenueChart.Labels.Add(month.ToString("MMM yyyy"));
                
                var monthRevenue = instructorOrderItems
                    .Where(oi => oi.Order.OrderDate.Year == month.Year && oi.Order.OrderDate.Month == month.Month)
                    .Sum(oi => oi.FinalPrice);
                    
                monthlyRevenueChart.Data.Add(monthRevenue);
            }

            return new InstructorDashboardResponse
            {
                TotalCourses = instructorCourses.Count,
                TotalStudents = totalStudents,
                TotalRevenue = totalRevenue,
                AverageCourseRating = averageRating,
                TotalReviewsCount = totalReviews,
                UnansweredDiscussionsCount = unansweredCount,
                EnrollmentTrendsChart = enrollmentTrendsChart,
                RevenuePerCourseChart = revenuePerCourseChart,
                RatingsDistributionChart = ratingsDistributionChart,
                MonthlyRevenueChart = monthlyRevenueChart
            };
        }

        public async Task<IEnumerable<CourseResponse>> GetCoursesAsync(Guid instructorGuid)
        {
            var user = await _userRepository.Get(instructorGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), instructorGuid);

            var allCourses = await _courseRepository.GetCoursesWithDetailsAsync();
            var instructorCourses = allCourses.Where(c => c.InstructorId == user.Id).ToList();
            var courseIds = instructorCourses.Select(c => c.Id).ToList();

            var enrollments = await _enrollmentRepository.GetEnrollmentsByCourseIdsAsync(courseIds);
            var reviews = await _reviewRepository.GetReviewsByCourseIdsAsync(courseIds);

            var response = _mapper.Map<IEnumerable<CourseResponse>>(instructorCourses).ToList();

            foreach (var resp in response)
            {
                resp.StudentsCount = enrollments.Count(e => e.CourseId == resp.Id);
                var courseReviews = reviews.Where(r => r.CourseId == resp.Id).ToList();
                resp.Rating = courseReviews.Any() ? courseReviews.Average(r => r.Rating) : 0.0;
            }

            return response;
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

        public async Task<IEnumerable<InstructorDiscussionResponse>> GetInstructorDiscussionsAsync(Guid instructorGuid, bool? unansweredOnly = null)
        {
            var user = await _userRepository.Get(instructorGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), instructorGuid);

            var allCourses = await _courseRepository.GetCoursesWithDetailsAsync();
            var instructorCourses = allCourses.Where(c => c.InstructorId == user.Id).ToList();
            if (!instructorCourses.Any())
            {
                return Enumerable.Empty<InstructorDiscussionResponse>();
            }

            var courseIds = instructorCourses.Select(c => c.Id).ToList();
            var discussions = await _discussionRepository.GetDiscussionsByCourseIdsAsync(courseIds);

            var responseList = discussions.Select(d => new InstructorDiscussionResponse
            {
                DiscussionGuid = d.ExternalId,
                CourseGuid = d.Course.ExternalId,
                CourseTitle = d.Course.Title,
                LectureTitle = d.Lecture?.Title,
                StudentName = d.User != null ? $"{d.User.FirstName} {d.User.LastName}".Trim() : string.Empty,
                Title = d.Title,
                Content = d.Content,
                CreatedAt = d.CreatedAt,
                RepliesCount = d.Replies?.Count ?? 0,
                IsAnswered = d.Replies != null && d.Replies.Any(r => r.UserId == user.Id)
            });

            if (unansweredOnly == true)
            {
                responseList = responseList.Where(r => !r.IsAnswered);
            }

            return responseList.ToList();
        }
    }
}
