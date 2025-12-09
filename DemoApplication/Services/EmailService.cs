using System.Net;
using System.Net.Mail;

namespace DemoApplication.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
                var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var appBaseUrl = _configuration["AppSettings:BaseUrl"];

                // Create reset link
                var resetLink = $"{appBaseUrl}/Account/ForgotChangePassword?token={WebUtility.UrlEncode(resetToken)}&email={WebUtility.UrlEncode(toEmail)}";

                // Create email message
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail),
                    Subject = "Password Reset Request",
                    Body = $@"
                    <html>
                    <body>
                        <h2>Password Reset Request</h2>
                        <p>Dear {userName},</p>
                        <p>You have requested to reset your password. Please click the link below to reset your password:</p>
                        <p><a href='{resetLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
                        <p>This link will expire in 24 hours.</p>
                        <p>If you did not request a password reset, please ignore this email.</p>
                        <br>
                        <p>Best regards,<br>Demo Application Team</p>
                    </body>
                    </html>",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                // Configure SMTP client
                using var smtpClient = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                    EnableSsl = true
                };

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation($"Password reset email sent to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send password reset email to {toEmail}");
                throw;
            }
        }
    }
}
