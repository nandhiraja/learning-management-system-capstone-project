using System;
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
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICartService _cartService;
        private readonly IMapper _mapper;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IOrderRepository orderRepository,
            IEnrollmentRepository enrollmentRepository,
            IUserRepository userRepository,
            ICartService cartService,
            IMapper mapper)
        {
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
            _enrollmentRepository = enrollmentRepository;
            _userRepository = userRepository;
            _cartService = cartService;
            _mapper = mapper;
        }

        public async Task<PaymentResponse> CreatePaymentAsync(PaymentCreateRequest request)
        {
            var order = await _orderRepository.Get(request.OrderId);
            if (order == null)
                throw new NotFoundException(nameof(Order), request.OrderId);

            if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var method))
                throw new ArgumentException($"Invalid payment method: {request.PaymentMethod}");

            // Create a pending payment
            var payment = new Payment
            {
                OrderId = order.Id,
                Amount = order.Amount,
                PaymentMethod = method,
                TransactionId = Guid.NewGuid().ToString(),
                Status = PaymentStatus.Pending,
                PaymentDate = DateTime.UtcNow
            };

            var createdPayment = await _paymentRepository.Create(payment);

            var response = _mapper.Map<PaymentResponse>(createdPayment);
            // Mock payment url redirecting to sandbox verification
            response.PaymentUrl = $"https://checkout.sandbox.lms.com/pay/{payment.TransactionId}?paymentId={createdPayment.Id}";

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
                var jsonStr = System.Text.Json.JsonSerializer.Serialize(gatewayPayload);
                var doc = System.Text.Json.JsonDocument.Parse(jsonStr);
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

            bool isSuccess = status.Equals("Success", StringComparison.OrdinalIgnoreCase) || 
                              status.Equals("Succeeded", StringComparison.OrdinalIgnoreCase) ||
                              status.Equals("Completed", StringComparison.OrdinalIgnoreCase);

            if (isSuccess)
            {
                payment.Status = PaymentStatus.Success;
                await _paymentRepository.Update(payment);

                var order = await _orderRepository.GetOrderWithDetailsAsync(payment.OrderId);
                if (order != null)
                {
                    order.Status = OrderStatus.Completed;
                    await _orderRepository.Update(order);

                    // Create enrollment for each course
                    foreach (var item in order.OrderItems)
                    {
                        var enrollment = new Enrollment
                        {
                            CourseId = item.CourseId,
                            UserId = order.UserId,
                            OrderItemId = item.Id,
                            Status = EnrollmentStatus.Active,
                            EnrolledAt = DateTime.UtcNow
                        };
                        await _enrollmentRepository.Create(enrollment);
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

            return true;
        }
    }
}
