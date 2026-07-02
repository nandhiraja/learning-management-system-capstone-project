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

            return new AdminDashboardResponse
            {
                Users = totalUsers,
                Courses = totalCourses,
                Orders = totalOrders,
                TotalRevenue = totalRevenue,
                PendingCoursesCount = pendingCoursesCount,
                BlockedUsersCount = blockedUsersCount,
                InstructorsCount = instructorsCount,
                StudentsCount = studentsCount
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

