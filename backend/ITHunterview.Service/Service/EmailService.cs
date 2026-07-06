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

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
        {
            var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:3000";
            var resetLink = $"{frontendUrl}/auth/reset-password?token={resetToken}&email={Uri.EscapeDataString(toEmail)}";

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

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                smtp["FromName"] ?? "ITHunterView",
                smtp["FromEmail"] ?? smtp["Username"]));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                smtp["Host"],
                int.Parse(smtp["Port"] ?? "587"),
                SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtp["Username"], smtp["Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
