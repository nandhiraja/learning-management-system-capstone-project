using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using LMS.Core.Enums;
using LMS.Core.Exception;
using LMS.Core.Models;
using LMS.DAL.Interfaces;
using LMS.DAL.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LMS.BLL.Services.Orders.Gateways;

namespace LMS.BLL.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICartService _cartService;
        private readonly IMapper _mapper;
        private readonly IEnumerable<IPaymentGateway> _gateways;
        private readonly LMSDBContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentService> _logger;
        private readonly INotificationService _notificationService;
        private readonly ICourseRepository _courseRepository;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IOrderRepository orderRepository,
            IEnrollmentRepository enrollmentRepository,
            IUserRepository userRepository,
            ICartService cartService,
            IMapper mapper,
            IEnumerable<IPaymentGateway> gateways,
            LMSDBContext context,
            IConfiguration configuration,
            ILogger<PaymentService> logger,
            INotificationService notificationService,
            ICourseRepository courseRepository)
        {
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
            _enrollmentRepository = enrollmentRepository;
            _userRepository = userRepository;
            _cartService = cartService;
            _mapper = mapper;
            _gateways = gateways;
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _notificationService = notificationService;
            _courseRepository = courseRepository;
        }

        private IPaymentGateway GetGateway(PaymentMethod method)
        {
            var gateway = _gateways.FirstOrDefault(g => g.SupportedMethod == method);
            if (gateway != null)
                return gateway;

            // Fallback to DefaultMockPaymentGateway
            var fallback = _gateways.FirstOrDefault(g => g is DefaultMockPaymentGateway);
            if (fallback != null)
                return fallback;

            throw new InvalidOperationException($"No payment gateway registered for method: {method}");
        }

        public async Task<PaymentResponse> CreatePaymentAsync(PaymentCreateRequest request)
        {
            var order = await _orderRepository.Get(request.OrderId);
            if (order == null)
                throw new NotFoundException(nameof(Order), request.OrderId);

            if (order.Status == OrderStatus.Completed)
                throw new InvalidOperationException("This order has already been completed.");

            // Check if there is already a successful payment record for this order
            var existingPayment = await _paymentRepository.GetPaymentByOrderIdAsync(order.Id);
            if (existingPayment != null && existingPayment.Status == PaymentStatus.Success)
            {
                throw new InvalidOperationException("Payment for this order has already been completed.");
            }

            if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var method))
                throw new ArgumentException($"Invalid payment method: {request.PaymentMethod}");

            var currency = _configuration["PayPal:Currency"] ?? "USD";
            var gateway = GetGateway(method);
            var (transactionId, paymentUrl) = await gateway.CreateOrderAsync(order.Id, order.Amount, currency);

            // Create payment record
            var payment = new Payment
            {
                OrderId = order.Id,
                Amount = order.Amount,
                PaymentMethod = method,
                TransactionId = transactionId ?? Guid.NewGuid().ToString(),
                Status = PaymentStatus.Pending,
                PaymentDate = DateTime.UtcNow
            };

            var createdPayment = await _paymentRepository.Create(payment);

            var response = _mapper.Map<PaymentResponse>(createdPayment);
            response.PaymentUrl = paymentUrl;

            return response;
        }

        public async Task<PaymentResponse?> GetPaymentByIdAsync(int paymentId)
        {
            var payment = await _paymentRepository.Get(paymentId);
            if (payment == null) return null;
            return _mapper.Map<PaymentResponse>(payment);
        }

        public async Task<bool> VerifyPaymentAsync(object gatewayPayload)
        {
            if (gatewayPayload == null) return false;

            int paymentId = 0;
            int orderId = 0;
            string? transactionId = null;
            string status = "Success";

            try
            {
                var jsonStr = JsonSerializer.Serialize(gatewayPayload);
                var doc = JsonDocument.Parse(jsonStr);
                var root = doc.RootElement;

                // Try to find paymentId
                if (root.TryGetProperty("paymentId", out var pIdProp) || root.TryGetProperty("PaymentId", out pIdProp))
                {
                    paymentId = pIdProp.GetInt32();
                }
                
                // Try to find orderId
                if (root.TryGetProperty("orderId", out var oIdProp) || root.TryGetProperty("OrderId", out oIdProp))
                {
                    orderId = oIdProp.GetInt32();
                }

                // Try to find transactionId
                if (root.TryGetProperty("transactionId", out var tIdProp) || root.TryGetProperty("TransactionId", out tIdProp))
                {
                    transactionId = tIdProp.GetString();
                }

                // Try to find status
                if (root.TryGetProperty("status", out var sProp) || root.TryGetProperty("Status", out sProp))
                {
                    status = sProp.GetString() ?? "Success";
                }
            }
            catch
            {
                // Fallback / Parse failure
                return false;
            }

            Payment? payment = null;

            if (paymentId > 0)
            {
                payment = await _paymentRepository.Get(paymentId);
            }
            else if (orderId > 0)
            {
                payment = await _paymentRepository.GetPaymentByOrderIdAsync(orderId);
            }
            else if (!string.IsNullOrEmpty(transactionId))
            {
                payment = await _paymentRepository.GetPaymentByTransactionIdAsync(transactionId);
            }

            if (payment == null) return false;

            // Idempotency: if payment is already processed, skip re-verification and return true
            if (payment.Status != PaymentStatus.Pending)
            {
                _logger.LogInformation($"Payment {payment.Id} has already been processed with status: {payment.Status}. Skipping duplicate request.");
                return true;
            }

            var gateway = GetGateway(payment.PaymentMethod);
            bool isSuccess = await gateway.VerifyPaymentAsync(payment.TransactionId, status, gatewayPayload);

            // Execute operations under a database transaction to ensure consistency
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (isSuccess)
                {
                    payment.Status = PaymentStatus.Success;
                    await _paymentRepository.Update(payment);

                    var order = await _orderRepository.GetOrderWithDetailsAsync(payment.OrderId);
                    if (order != null)
                    {
                        order.Status = OrderStatus.Completed;
                        await _orderRepository.Update(order);

                        // Fetch existing enrollments to prevent DB unique key constraint conflicts
                        var existingEnrollments = await _enrollmentRepository.GetEnrollmentsByUserIdAsync(order.UserId);
                        var enrolledCourseIds = existingEnrollments.Select(e => e.CourseId).ToHashSet();

                        // Create enrollment for each course
                        foreach (var item in order.OrderItems)
                        {
                            if (enrolledCourseIds.Contains(item.CourseId))
                            {
                                _logger.LogInformation($"User {order.UserId} is already enrolled in Course {item.CourseId}. Skipping duplicate creation.");
                                continue;
                            }

                            var enrollment = new Enrollment
                            {
                                CourseId = item.CourseId,
                                UserId = order.UserId,
                                OrderItemId = item.Id,
                                Status = EnrollmentStatus.Active,
                                EnrolledAt = DateTime.UtcNow
                            };
                            await _enrollmentRepository.Create(enrollment);

                            var course = await _courseRepository.Get(item.CourseId);
                            if (course != null)
                            {
                                string emailBody = $@"
                                    <h2>Enrollment Successful</h2>
                                    <p>You have successfully enrolled in the course: <strong>{course.Title}</strong>.</p>
                                    <p>Happy learning!<br/>LMS Team</p>";
                                await _notificationService.SendEmailAsync("nandhiraja16@gmail.com", "Enrollment Successful", emailBody);
                            }
                        }

                        // Clear the user's cart
                        var user = await _userRepository.GetUserWithRoleAsync(order.UserId);
                        if (user != null)
                        {
                            await _cartService.ClearCartAsync(user.ExternalId);
                        }
                    }
                }
                else
                {
                    payment.Status = PaymentStatus.Failed;
                    await _paymentRepository.Update(payment);

                    var order = await _orderRepository.Get(payment.OrderId);
                    if (order != null)
                    {
                        order.Status = OrderStatus.Cancelled;
                        await _orderRepository.Update(order);
                    }
                }

                await dbTransaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, $"Transaction failed while verifying payment {payment.Id}");
                throw;
            }

            return true;
        }
    }
}
