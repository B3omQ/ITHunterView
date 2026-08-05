using System;
using System.Threading.Tasks;
using ITHunterview.Service.Interface.Service;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace ITHunterview.Service.Service
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendVerificationEmailAsync(string toEmail, string verificationToken)
        {
            var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:3000";
            var verifyLink = $"{frontendUrl}/verify-email?token={verificationToken}";

            var subject = "Verify your ITHunterView account";
            var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto;'>
                    <h2 style='color: #4F46E5;'>Verify your account</h2>
                    <p>Thank you for registering an account at <strong>ITHunterView</strong>.</p>
                    <p>Click the button below to verify your email address:</p>
                    <a href='{verifyLink}' 
                       style='display:inline-block; padding:12px 24px; background:#4F46E5; color:white; 
                              text-decoration:none; border-radius:6px; font-weight:bold; margin: 16px 0;'>
                        Verify Email
                    </a>
                    <p style='color: #6B7280; font-size: 13px;'>This link is valid for 24 hours.</p>
                    <p style='color: #6B7280; font-size: 13px;'>If you did not create an account, please ignore this email.</p>
                </div>";

            await SendEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string? accountEmail = null)
        {
            var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:3000";
            var targetAccount = accountEmail ?? toEmail;
            var resetLink = $"{frontendUrl}/reset-password?token={resetToken}&email={Uri.EscapeDataString(targetAccount)}";

            var subject = "Reset your ITHunterView password";
            var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto;'>
                    <h2 style='color: #4F46E5;'>Reset Password</h2>
                    <p>We received a request to reset the password for the account associated with this email.</p>
                    <p>Click the button below to reset your password:</p>
                    <a href='{resetLink}'
                       style='display:inline-block; padding:12px 24px; background:#4F46E5; color:white; 
                              text-decoration:none; border-radius:6px; font-weight:bold; margin: 16px 0;'>
                        Reset Password
                    </a>
                    <p style='color: #6B7280; font-size: 13px;'>This link is valid for 15 minutes.</p>
                    <p style='color: #6B7280; font-size: 13px;'>If you did not request a password reset, please ignore this email.</p>
                </div>";

            await SendEmailAsync(toEmail, subject, htmlBody);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var smtp = _configuration.GetSection("SmtpSettings");
            var host = smtp["Host"] ?? "smtp.gmail.com";
            var portStr = smtp["Port"] ?? "587";
            int.TryParse(portStr, out int port);
            if (port <= 0) port = 587;

            var username = (smtp["Username"] ?? "").Trim();
            var password = (smtp["Password"] ?? "").Trim('"').Trim();

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                smtp["FromName"] ?? "ITHunterView",
                smtp["FromEmail"] ?? username));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            // Thiết lập Timeout 10 giây thay vì 100 giây mặc định để tránh treo HTTP Request khi Port bị chặn
            client.Timeout = 10000;

            // Xác định SecureSocketOptions dựa trên Port (Port 465 = SslOnConnect, Port 587 = StartTls)
            var socketOption = port == 465 
                ? SecureSocketOptions.SslOnConnect 
                : (port == 587 ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);

            try
            {
                await client.ConnectAsync(host, port, socketOption);
                if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                {
                    await client.AuthenticateAsync(username, password);
                }
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] SMTP SendEmailAsync failed to {toEmail} via {host}:{port}: {ex.Message}");
                throw new InvalidOperationException($"Không thể gửi email qua SMTP ({host}:{port}): {ex.Message}", ex);
            }
        }
    }
}
