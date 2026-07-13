using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using LMS.BLL.Interfaces;

namespace LMS.PL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireAuth")]
    [EnableRateLimiting("api-limiter")]
    public class NotificationsController : ControllerBase
    {
        private readonly IRealTimeNotificationService _notificationService;

        public NotificationsController(IRealTimeNotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private int CurrentUserId
        {
            get
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                return int.TryParse(userIdClaim, out var id) ? id : 0;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = CurrentUserId;
            if (userId == 0) return Unauthorized();

            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            return Ok(notifications);
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = CurrentUserId;
            if (userId == 0) return Unauthorized();

            await _notificationService.MarkAsReadAsync(id, userId);
            return NoContent();
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = CurrentUserId;
            if (userId == 0) return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(userId);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var userId = CurrentUserId;
            if (userId == 0) return Unauthorized();

            await _notificationService.DeleteNotificationAsync(id, userId);
            return NoContent();
        }
    }
}
