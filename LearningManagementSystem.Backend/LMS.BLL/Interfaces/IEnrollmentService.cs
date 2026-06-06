using LMS.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IEnrollmentService
    {
        Task<IEnumerable<EnrollmentResponse>> GetUserCoursesAsync(Guid userGuid);
        Task<EnrollmentResponse?> GetEnrollmentDetailsAsync(Guid userGuid, Guid courseGuid);
    }
}
