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
    [Authorize]
    [EnableRateLimiting("api-limiter")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
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
    }
}
