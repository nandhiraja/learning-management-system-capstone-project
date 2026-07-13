using LMS.BLL.Interfaces;
using LMS.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.RateLimiting;

namespace LMS.PL.Controllers
{
    [ApiController]
    [Route("api/payments")]
    [Authorize(Policy = "StudentAccess")]
    [EnableRateLimiting("api-limiter")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ICartService _cartService;
        private readonly IConfiguration _configuration;

        public PaymentController(IPaymentService paymentService, ICartService cartService, IConfiguration configuration)
        {
            _paymentService = paymentService;
            _cartService = cartService;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentCreateRequest request)
        {
            var response = await _paymentService.CreatePaymentAsync(request);
            return Ok(response);
        }

        [HttpGet("{paymentId}")]
        public async Task<IActionResult> GetPayment(int paymentId)
        {
            var response = await _paymentService.GetPaymentByIdAsync(paymentId);
            if (response == null) return NotFound();
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("verify")]
        public async Task<IActionResult> ProcessWebhook([FromBody] object payload)
        {
            var success = await _paymentService.VerifyPaymentAsync(payload);
            if (!success) return BadRequest();
            return Ok(new { message = "Processed" });
        }

        [AllowAnonymous]
        [HttpGet("paypal-callback")]
        public async Task<IActionResult> PayPalCallback([FromQuery] string paymentId, [FromQuery] string token, [FromQuery] string PayerID)
        {
            var frontendUrl = _configuration["FrontendBaseUrl"] ?? "http://localhost:4200";
            
            var payload = new System.Collections.Generic.Dictionary<string, object>
            {
                { "transactionId", token },
                { "status", "Success" }
            };
            var success = await _paymentService.VerifyPaymentAsync(payload);
            if (success)
            {
                return Redirect($"{frontendUrl}/learning/dashboard?paymentStatus=success");
            }
            return Redirect($"{frontendUrl}/cart?paymentStatus=failed");
        }

        [AllowAnonymous]
        [HttpGet("paypal-cancel")]
        public IActionResult PayPalCancel()
        {
            var frontendUrl = _configuration["FrontendBaseUrl"] ?? "http://localhost:4200";
            return Redirect($"{frontendUrl}/cart?paymentStatus=cancelled");
        }
    }
}
