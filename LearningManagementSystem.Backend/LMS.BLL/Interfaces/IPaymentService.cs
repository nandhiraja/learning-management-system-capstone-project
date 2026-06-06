using LMS.Core.DTOs;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponse> CreatePaymentAsync(PaymentCreateRequest request);
        Task<bool> VerifyPaymentAsync(object gatewayPayload);
        Task<PaymentResponse?> GetPaymentByIdAsync(int paymentId);
    }
}
