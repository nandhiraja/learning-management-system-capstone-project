using LMS.Core.DTOs;
using System;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface ICertificateService
    {
        Task<CertificateResponse?> GetCertificateAsync(Guid courseGuid, Guid userGuid);
        Task<CertificateResponse> GenerateCertificateAsync(Guid courseGuid, Guid userGuid);
        Task<CertificateResponse?> GetCertificateByIdAsync(int certificateId);
        Task<IEnumerable<CertificateResponse>> GetCertificatesByUserAsync(Guid userGuid);
        Task<int> RegenerateAllCertificatesAsync();
    }
}

