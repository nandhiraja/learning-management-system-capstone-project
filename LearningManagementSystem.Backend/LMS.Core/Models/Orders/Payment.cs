using LMS.Core.Enums;

namespace LMS.Core.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string TransactionId { get; set; } = null!;
        public PaymentStatus Status { get; set; }
        public DateTime PaymentDate { get; set; }

        // Navigation property
        public Order Order { get; set; } = null!;
    }
}