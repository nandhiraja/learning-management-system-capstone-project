using System.Collections.Generic;
using System.Threading.Tasks;
using LMS.Core.Models;

namespace LMS.BLL.Interfaces
{
    public interface IRealTimeNotificationService
    {
        Task CreateAndSendNotificationAsync(int userId, string title, string message, string type);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId);
        Task MarkAsReadAsync(int notificationId, int userId);
        Task MarkAllAsReadAsync(int userId);
        Task DeleteNotificationAsync(int notificationId, int userId);
    }
}
