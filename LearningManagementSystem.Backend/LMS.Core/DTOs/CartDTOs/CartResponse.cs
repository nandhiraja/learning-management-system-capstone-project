using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace LMS.Core.DTOs
{
    public class CartResponse
    {
        public IEnumerable<CartItemResponse> Items { get; set; } = new List<CartItemResponse>();
        public decimal Total { get; set; }
    }

    public class CartItemResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public decimal Price { get; set; }
    }

    public class CartItemRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "CourseId must be a positive integer.")]
        public int CourseId { get; set; }
    }
}
