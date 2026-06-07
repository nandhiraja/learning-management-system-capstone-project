using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LMS.PL.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;

        public OrderController(ICartService cartService, IOrderService orderService, IPaymentService paymentService)
        {
            _cartService = cartService;
            _orderService = orderService;
            _paymentService = paymentService;
        }

        protected Guid CurrentUserGuid
        {
            get
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return Guid.TryParse(idClaim, out var guid) ? guid : Guid.Empty;
            }
        }

        // --- Cart Endpoints ---

        [HttpGet("cart")]
        public async Task<IActionResult> GetCart()
        {
            var cart = await _cartService.GetCartAsync(CurrentUserGuid);
            return Ok(cart);
        }

        [HttpPost("cart")]
        public async Task<IActionResult> AddToCart([FromBody] CartItemRequest request)
        {
            var success = await _cartService.AddToCartAsync(CurrentUserGuid, request.CourseId);
            if (!success) return BadRequest("Already in cart or invalid course");
            return Ok(new { message = "Added to cart" });
        }

        [HttpDelete("cart/{courseId}")]
        public async Task<IActionResult> RemoveFromCart(int courseId)
        {
            var success = await _cartService.RemoveFromCartAsync(CurrentUserGuid, courseId);
            if (!success) return NotFound();
            return Ok(new { message = "Removed" });
        }

        // --- Order Endpoints ---

        [HttpPost("orders")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateRequest request)
        {
            var order = await _orderService.CreateOrderAsync(CurrentUserGuid, request);
            return Ok(order);
        }

        [HttpGet("orders/{orderId}")]
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null) return NotFound();
            return Ok(order);
        }

        // --- Payment Endpoints ---

        [HttpPost("payments")]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentCreateRequest request)
        {
            var response = await _paymentService.CreatePaymentAsync(request);
            return Ok(response);
        }

        [HttpGet("payments/{paymentId}")]
        public async Task<IActionResult> GetPayment(int paymentId)
        {
            var response = await _paymentService.GetPaymentByIdAsync(paymentId);
            if (response == null) return NotFound();
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("payments/verify")]
        public async Task<IActionResult> ProcessWebhook([FromBody] object payload)
        {
            var success = await _paymentService.VerifyPaymentAsync(payload);
            if (!success) return BadRequest();
            return Ok(new { message = "Processed" });
        }
    }
}
