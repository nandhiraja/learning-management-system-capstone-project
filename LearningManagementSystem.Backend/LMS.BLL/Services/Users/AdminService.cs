using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
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

            return new AdminDashboardResponse
            {
                Users = totalUsers,
                Courses = totalCourses,
                Orders = totalOrders
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
            return _mapper.Map<IEnumerable<CourseResponse>>(courses);
        }
    }
}

