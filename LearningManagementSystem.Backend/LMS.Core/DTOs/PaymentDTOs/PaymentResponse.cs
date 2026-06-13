using System.ComponentModel.DataAnnotations;

namespace LMS.Core.DTOs
{
    public class PaymentCreateRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "OrderId must be a positive integer.")]
        public int OrderId { get; set; }

        [Required]
        [StringLength(50)]
        [RegularExpression("^(?i)(PayPal|CreditCard|Stripe|UPI|BankTransfer)$", ErrorMessage = "Invalid payment method. Allowed values: PayPal, CreditCard, Stripe, UPI, BankTransfer")]
        public string PaymentMethod { get; set; } = null!;
    }

    public class PaymentResponse
    {
        public int Id { get; set; }
        public string Status { get; set; } = null!;
        public decimal Amount { get; set; }
        public string? PaymentUrl { get; set; }
    }
}
