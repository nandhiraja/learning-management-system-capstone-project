using System.Threading.Tasks;
using LMS.Core.Enums;

namespace LMS.BLL.Interfaces
{
    public interface IPaymentGateway
    {
        PaymentMethod SupportedMethod { get; }
        Task<(string? TransactionId, string? PaymentUrl)> CreateOrderAsync(int orderId, decimal amount, string currency);
        Task<bool> VerifyPaymentAsync(string transactionId, string status, object rawPayload);
    }
}
