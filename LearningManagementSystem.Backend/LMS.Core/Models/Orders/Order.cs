using LMS.Core.Enums;

namespace LMS.Core.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Amount { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public IEnumerable<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public Payment? Payment { get; set; }
    }
}