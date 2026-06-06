namespace LMS.Core.DTOs
{
    public class PaymentCreateRequest
    {
        public int OrderId { get; set; }
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
