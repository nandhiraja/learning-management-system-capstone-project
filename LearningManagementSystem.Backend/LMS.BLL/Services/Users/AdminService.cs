using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using LMS.Core.Enums;
using LMS.Core.Models;
using LMS.DAL.Interfaces;

namespace LMS.BLL.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IMapper _mapper;

        public AdminService(
            IUserRepository userRepository,
            ICourseRepository courseRepository,
            IOrderRepository orderRepository,
            IPaymentRepository paymentRepository,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _courseRepository = courseRepository;
            _orderRepository = orderRepository;
            _paymentRepository = paymentRepository;
            _mapper = mapper;
        }

        public async Task<AdminDashboardResponse> GetDashboardDataAsync()
        {
            int totalUsers = await _userRepository.GetCountAsync();
            int totalCourses = await _courseRepository.GetCountAsync();
            int totalOrders = await _orderRepository.GetCountAsync();
            decimal totalRevenue = await _orderRepository.GetTotalRevenueAsync();
            int pendingCoursesCount = await _courseRepository.GetPendingCoursesCountAsync();
            int blockedUsersCount = await _userRepository.GetBlockedUsersCountAsync();

            var usersList = await _userRepository.GetAllAsync();
            int instructorsCount = usersList.Count(u => u.Role?.Name?.Equals("Instructor", StringComparison.OrdinalIgnoreCase) ?? false);
            int studentsCount = usersList.Count(u => u.Role?.Name?.Equals("Student", StringComparison.OrdinalIgnoreCase) ?? false);

            var allCourses = await _courseRepository.GetCoursesWithDetailsAsync();

            // 1. Platform Growth Chart (Users registered per month for last 6 months)
            var platformGrowthChart = new ChartDataDto();
            for (int i = 5; i >= 0; i--)
            {
                var month = DateTime.Now.AddMonths(-i);
                platformGrowthChart.Labels.Add(month.ToString("MMM yyyy"));
                platformGrowthChart.Data.Add(usersList.Count(u => u.CreatedAt.Year == month.Year && u.CreatedAt.Month == month.Month));
            }

            // 2. Revenue By Category Chart
            var revenueByCategoryChart = new ChartDataDto();
            var allCompletedOrderItems = await _orderRepository.GetAllCompletedOrderItemsAsync();
            
            var categoryRevenue = new Dictionary<string, decimal>();

            foreach (var orderItem in allCompletedOrderItems)
            {
                var course = allCourses.FirstOrDefault(c => c.Id == orderItem.CourseId);
                if (course != null && course.Category != null)
                {
                    if (!categoryRevenue.ContainsKey(course.Category.Name))
                        categoryRevenue[course.Category.Name] = 0;
                    categoryRevenue[course.Category.Name] += orderItem.FinalPrice;
                }
            }

            foreach (var kvp in categoryRevenue)
            {
                revenueByCategoryChart.Labels.Add(kvp.Key);
                revenueByCategoryChart.Data.Add(kvp.Value);
            }

            // 3. Course Status Chart
            var courseStatusChart = new ChartDataDto();
            var statusGroups = allCourses.GroupBy(c => c.Status);
            foreach (var group in statusGroups)
            {
                courseStatusChart.Labels.Add(group.Key.ToString());
                courseStatusChart.Data.Add(group.Count());
            }

            // 4. Monthly Revenue Chart (Revenue per month for last 6 months)
            var monthlyRevenueChart = new ChartDataDto();
            for (int i = 5; i >= 0; i--)
            {
                var month = DateTime.Now.AddMonths(-i);
                monthlyRevenueChart.Labels.Add(month.ToString("MMM yyyy"));
                
                var monthRevenue = allCompletedOrderItems
                    .Where(oi => oi.Order.OrderDate.Year == month.Year && oi.Order.OrderDate.Month == month.Month)
                    .Sum(oi => oi.FinalPrice);
                    
                monthlyRevenueChart.Data.Add(monthRevenue);
            }

            return new AdminDashboardResponse
            {
                Users = totalUsers,
                Courses = totalCourses,
                Orders = totalOrders,
                TotalRevenue = totalRevenue,
                PendingCoursesCount = pendingCoursesCount,
                BlockedUsersCount = blockedUsersCount,
                InstructorsCount = instructorsCount,
                StudentsCount = studentsCount,
                PlatformGrowthChart = platformGrowthChart,
                RevenueByCategoryChart = revenueByCategoryChart,
                CourseStatusChart = courseStatusChart,
                MonthlyRevenueChart = monthlyRevenueChart
            };
        }

        public async Task<IEnumerable<UserProfileResponse>> GetUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<UserProfileResponse>>(users);
        }

        public async Task<IEnumerable<OrderResponse>> GetOrdersAsync()
        {
            var orders = await _orderRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<OrderResponse>>(orders);
        }

        public async Task<IEnumerable<PaymentResponse>> GetPaymentsAsync()
        {
            var payments = await _paymentRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<PaymentResponse>>(payments);
        }

        public async Task<IEnumerable<CourseResponse>> GetPendingCoursesAsync()
        {
            var courses = await _courseRepository.GetPendingCoursesAsync();
            var responses = _mapper.Map<IEnumerable<CourseResponse>>(courses).ToList();

            foreach (var response in responses)
            {
                var course = courses.FirstOrDefault(c => c.ExternalId == response.ExternalId);
                if (course != null && course.OriginalCourseId.HasValue)
                {
                    var original = await _courseRepository.GetCourseWithDetailsAsync(course.OriginalCourseId.Value);
                    if (original != null)
                    {
                        response.OriginalCourseExternalId = original.ExternalId;
                        response.OriginalCourseDetails = _mapper.Map<CourseResponse>(original);
                    }
                }
            }

            return responses;
        }

        public async Task<IEnumerable<CourseResponse>> GetAdminCoursesAsync()
        {
            var courses = await _courseRepository.GetCoursesWithDetailsAsync();
            var filtered = courses.Where(c => c.Status != CourseStatus.Draft).ToList();
            return _mapper.Map<IEnumerable<CourseResponse>>(filtered);
        }
    }
}

