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
        Task<IEnumerable<CourseResponse>> GetAdminCoursesAsync();
    }


    public class AdminDashboardResponse
    {
        public int Users { get; set; }
        public int Courses { get; set; }
        public int Orders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingCoursesCount { get; set; }
        public int BlockedUsersCount { get; set; }
        public int InstructorsCount { get; set; }
        public int StudentsCount { get; set; }

        public ChartDataDto PlatformGrowthChart { get; set; } = new();
        public ChartDataDto RevenueByCategoryChart { get; set; } = new();
        public ChartDataDto CourseStatusChart { get; set; } = new();
        public ChartDataDto MonthlyRevenueChart { get; set; } = new();
    }
}
