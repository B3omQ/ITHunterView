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
            var verifyLink = $"{frontendUrl}/auth/verify-email?token={verificationToken}";

            var subject = "Xác thực tài khoản ITHunterView";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto;'>
                    <h2 style='color: #4F46E5;'>Xác thực tài khoản của bạn</h2>
                    <p>Cảm ơn bạn đã đăng ký tài khoản tại <strong>ITHunterView</strong>.</p>
                    <p>Nhấn vào nút bên dưới để xác thực địa chỉ email của bạn:</p>
                    <a href='{verifyLink}' 
                       style='display:inline-block; padding:12px 24px; background:#4F46E5; color:white; 
                              text-decoration:none; border-radius:6px; font-weight:bold; margin: 16px 0;'>
                        Xác thực Email
                    </a>
                    <p style='color: #6B7280; font-size: 13px;'>Liên kết này có hiệu lực trong 24 giờ.</p>
                    <p style='color: #6B7280; font-size: 13px;'>Nếu bạn không đăng ký tài khoản, hãy bỏ qua email này.</p>
                </div>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
        {
            var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:3000";
            var resetLink = $"{frontendUrl}/auth/reset-password?token={resetToken}&email={Uri.EscapeDataString(toEmail)}";

            var subject = "Đặt lại mật khẩu ITHunterView";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto;'>
                    <h2 style='color: #4F46E5;'>Đặt lại mật khẩu</h2>
                    <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản liên kết với email này.</p>
                    <p>Nhấn vào nút bên dưới để đặt lại mật khẩu:</p>
                    <a href='{resetLink}'
                       style='display:inline-block; padding:12px 24px; background:#4F46E5; color:white; 
                              text-decoration:none; border-radius:6px; font-weight:bold; margin: 16px 0;'>
                        Đặt lại mật khẩu
                    </a>
                    <p style='color: #6B7280; font-size: 13px;'>Liên kết này có hiệu lực trong 15 phút.</p>
                    <p style='color: #6B7280; font-size: 13px;'>Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.</p>
                </div>";

            await SendEmailAsync(toEmail, subject, body);
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
