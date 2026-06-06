using System;
using System.Collections.Generic;

namespace LMS.Core.DTOs
{
    public class OrderCreateRequest
    {
        public IEnumerable<int> CourseIds { get; set; } = new List<int>();
    }

    public class OrderResponse
    {
        public int Id { get; set; }
        public Guid ExternalId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = null!;
    }
}
