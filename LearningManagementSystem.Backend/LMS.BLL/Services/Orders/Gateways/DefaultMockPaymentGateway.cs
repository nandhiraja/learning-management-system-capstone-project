using System;
using System.Threading.Tasks;
using LMS.BLL.Interfaces;
using LMS.Core.Enums;
using Microsoft.Extensions.Configuration;

namespace LMS.BLL.Services.Orders.Gateways
{
    public class DefaultMockPaymentGateway : IPaymentGateway
    {
        private readonly IConfiguration _configuration;

        public DefaultMockPaymentGateway(IConfiguration configuration)
        {
            _configuration = configuration;
        }
      
        public PaymentMethod SupportedMethod => PaymentMethod.CreditCard;

        public Task<(string? TransactionId, string? PaymentUrl)> CreateOrderAsync(int orderId, decimal amount, string currency)
        {
            string transactionId = Guid.NewGuid().ToString();
            string configuredUrl = _configuration["PayPal:MockCheckoutUrl"] ?? "https://localhost:7165/api/payments/verify";
            string paymentUrl = $"{configuredUrl}?paymentId={orderId}&transactionId={transactionId}&status=Success";
            return Task.FromResult<(string?, string?)>((transactionId, paymentUrl));
        }

        public Task<bool> VerifyPaymentAsync(string transactionId, string status, object rawPayload)
        {
            bool isSuccess = status.Equals("Success", StringComparison.OrdinalIgnoreCase) ||
                             status.Equals("Succeeded", StringComparison.OrdinalIgnoreCase) ||
                             status.Equals("Completed", StringComparison.OrdinalIgnoreCase);

            return Task.FromResult(isSuccess);
        }
    }
}
