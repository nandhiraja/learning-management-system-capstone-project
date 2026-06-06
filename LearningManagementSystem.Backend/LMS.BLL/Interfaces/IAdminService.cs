using LMS.Core.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface IAdminService
    {
        Task<AdminDashboardResponse> GetDashboardDataAsync();
        Task<IEnumerable<UserProfileResponse>> GetUsersAsync();
        Task<IEnumerable<OrderResponse>> GetOrdersAsync();
        Task<IEnumerable<PaymentResponse>> GetPaymentsAsync();
        Task<IEnumerable<CourseResponse>> GetPendingCoursesAsync();
    }


    public class AdminDashboardResponse
    {
        public int Users { get; set; }
        public int Courses { get; set; }
        public int Orders { get; set; }
    }
}
