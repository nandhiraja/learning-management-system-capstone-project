using System.Threading.Tasks;

namespace LMS.BLL.Interfaces
{
    public interface INotificationService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }
}
