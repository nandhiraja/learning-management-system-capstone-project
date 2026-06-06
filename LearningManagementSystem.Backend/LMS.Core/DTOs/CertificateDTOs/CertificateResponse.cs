using System;

namespace LMS.Core.DTOs
{
    public class CertificateResponse
    {
        public int Id { get; set; }
        public DateTime IssuedDate { get; set; }
        public string CertificateUrl { get; set; } = null!;
    }
}
