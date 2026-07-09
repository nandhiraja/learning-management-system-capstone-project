namespace LMS.Core.DTOs
{
    public class ChartDataDto
    {
        public System.Collections.Generic.List<string> Labels { get; set; } = new();
        public System.Collections.Generic.List<decimal> Data { get; set; } = new();
    }
}
