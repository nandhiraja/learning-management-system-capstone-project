using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LMS.BLL.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace LMS.BLL.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationService> _logger;

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

            string htmlTemplate = $@"
                        <!DOCTYPE html>
                        <html>
                        <head>
                            <meta charset='utf-8'>
                            <style>
                                body {{ font-family: 'Inter', 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7f6; margin: 0; padding: 20px; color: #333333; }}
                                .container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.05); border: 1px solid #eaeaea; }}
                                .header {{ background-color: #4f46e5; padding: 30px 20px; text-align: center; color: #ffffff; }}
                                .header h1 {{ margin: 0; font-size: 24px; font-weight: 600; letter-spacing: 0.5px; }}
                                .content {{ padding: 40px 30px; line-height: 1.6; font-size: 16px; color: #4b5563; }}
                                .content h2 {{ color: #111827; font-size: 20px; margin-top: 0; margin-bottom: 20px; }}
                                .content p {{ margin-bottom: 16px; }}
                                .footer {{ background-color: #f9fafb; padding: 20px; text-align: center; font-size: 13px; color: #6b7280; border-top: 1px solid #f3f4f6; }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <div class='header'>
                                    <h1>LMS Platform</h1>
                                </div>
                                <div class='content'>
                                    {body}
                                </div>
                                <div class='footer'>
                                    &copy; {DateTime.UtcNow.Year} Learning Management System. All rights reserved.<br/>
                                    This is an automated message. Please do not reply.
                                </div>
                            </div>
                        </body>
                        </html>";

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("LMS", fromAddress));
                message.To.Add(new MailboxAddress("", fixedRecipient));
                message.Subject = subject;

                var builder = new BodyBuilder { HtmlBody = htmlTemplate };
                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                
                var secureOption = enableSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None;
                await client.ConnectAsync(host, port, secureOption);

                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    await client.AuthenticateAsync(username, password);
                }

                _logger.LogInformation("Sending email to {recipient} via SMTP host {host}:{port}", fixedRecipient, host, port);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {recipient}.", fixedRecipient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {recipient} via SMTP host {host}:{port}", fixedRecipient, host, port);
            }
        }
    }
}
