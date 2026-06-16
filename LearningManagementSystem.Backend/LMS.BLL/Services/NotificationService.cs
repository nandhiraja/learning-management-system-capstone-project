using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LMS.BLL.Interfaces;

namespace LMS.BLL.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationService> _logger;

        public Func<string, int, SmtpClient>? SmtpClientFactory { get; set; }
        public Func<MailMessage, SmtpClient, Task>? SendMailAsyncAction { get; set; }

        public NotificationService(IConfiguration configuration, ILogger<NotificationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            string fixedRecipient = "nandhiraja16@gmail.com";

            var host = _configuration["Smtp:Host"] ?? "localhost";
            var portStr = _configuration["Smtp:Port"] ?? "25";
            int.TryParse(portStr, out var port);
            if (port <= 0) port = 25;

            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var fromAddress = _configuration["Smtp:FromAddress"] ?? "no-reply@lms.com";
            var enableSslStr = _configuration["Smtp:EnableSsl"] ?? "false";
            bool.TryParse(enableSslStr, out var enableSsl);

            try
            {
                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromAddress),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(fixedRecipient);

                using var smtpClient = SmtpClientFactory != null 
                    ? SmtpClientFactory(host, port) 
                    : new SmtpClient(host, port);

                smtpClient.EnableSsl = enableSsl;

                if (!string.IsNullOrEmpty(username))
                {
                    smtpClient.Credentials = new NetworkCredential(username, password);
                }

                _logger.LogInformation("Sending email to {recipient} via SMTP host {host}:{port}", fixedRecipient, host, port);
                
                var sendTask = SendMailAsyncAction != null 
                    ? SendMailAsyncAction(mailMessage, smtpClient) 
                    : smtpClient.SendMailAsync(mailMessage);
                await sendTask;

                _logger.LogInformation("Email sent successfully to {recipient}.", fixedRecipient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {recipient} via SMTP host {host}:{port}", fixedRecipient, host, port);
            }
        }
    }
}
