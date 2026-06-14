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
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IMapper _mapper;

        public OrderService(
            IOrderRepository orderRepository,
            IUserRepository userRepository,
            ICourseRepository courseRepository,
            IEnrollmentRepository enrollmentRepository,
            IMapper mapper)
        {
            _orderRepository = orderRepository;
            _userRepository = userRepository;
            _courseRepository = courseRepository;
            _enrollmentRepository = enrollmentRepository;
            _mapper = mapper;
        }

        public async Task<OrderResponse> CreateOrderAsync(Guid userGuid, OrderCreateRequest request)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            if (!user.IsActive)
                throw new InvalidOperationException("Your account is deactivated. Cannot place order.");

            if (request.CourseIds == null || !request.CourseIds.Any())
                throw new InvalidOperationException("Cannot create an order with no courses.");

            // Prevent duplicate courses in the same checkout order
            if (request.CourseIds.Distinct().Count() != request.CourseIds.Count())
                throw new ArgumentException("Duplicate courses in order creation request are not allowed.");

            // Check existing enrollments to prevent purchasing courses the user already owns
            var existingEnrollments = await _enrollmentRepository.GetEnrollmentsByUserIdAsync(user.Id);
            var enrolledCourseIds = existingEnrollments.Select(e => e.CourseId).ToHashSet();

            var orderItems = new List<OrderItem>();
            decimal totalAmount = 0;

            foreach (var courseId in request.CourseIds)
            {
                var course = await _courseRepository.Get(courseId);
                if (course == null)
                    throw new NotFoundException(nameof(Course), courseId);

                if (course.Status != CourseStatus.Published)
                    throw new InvalidOperationException($"The course '{course.Title}' is not published and cannot be purchased.");

                if (enrolledCourseIds.Contains(course.Id))
                    throw new InvalidOperationException($"You are already enrolled in the course: {course.Title}");

                decimal discountPrice = course.Price * course.DiscountPercentage / 100m;
                decimal finalPrice = course.Price - discountPrice;

                orderItems.Add(new OrderItem
                {
                    CourseId = course.Id,
                    Price = course.Price,
                    DiscountPercentage = course.DiscountPercentage,
                    FinalPrice = finalPrice
                });

                totalAmount += finalPrice;
            }

            var order = new Order
            {
                ExternalId = Guid.NewGuid(),
                UserId = user.Id,
                Status = OrderStatus.Pending,
                OrderDate = DateTime.UtcNow,
                Amount = totalAmount,
                OrderItems = orderItems
            };

            var createdOrder = await _orderRepository.Create(order);
            return _mapper.Map<OrderResponse>(createdOrder);
        }

        public async Task<OrderResponse?> GetOrderByIdAsync(Guid orderGuid)
        {
            var order = await _orderRepository.GetByExternalIdAsync(orderGuid);
            if (order == null) return null;
            return _mapper.Map<OrderResponse>(order);
        }

        public async Task<IEnumerable<OrderResponse>> GetUserOrdersAsync(Guid userGuid)
        {
            var user = await _userRepository.Get(userGuid);
            if (user == null)
                throw new NotFoundException(nameof(User), userGuid);

            var orders = await _orderRepository.GetOrdersByUserIdAsync(user.Id);
            return _mapper.Map<IEnumerable<OrderResponse>>(orders);
        }
    }
}
