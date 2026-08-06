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
            var brevoApiKey = (_configuration["BrevoApiKey"] ?? _configuration["BrevoSettings:ApiKey"] ?? _configuration["Brevo__ApiKey"] ?? "").Trim();
            var resendApiKey = (_configuration["ResendApiKey"] ?? _configuration["ResendSettings:ApiKey"] ?? _configuration["Resend__ApiKey"] ?? "").Trim();
            var fromName = _configuration["SmtpSettings:FromName"] ?? "ITHunterView";
            var fromEmail = _configuration["SmtpSettings:FromEmail"] ?? _configuration["SmtpSettings:Username"] ?? "hainam1402004@gmail.com";

            // 1. Ưu tiên 1: Gửi qua Brevo (Sendinblue) HTTP API (Gửi được cho MỌI email người dùng miễn phí không cần Domain riêng)
            if (!string.IsNullOrWhiteSpace(brevoApiKey))
            {
                try
                {
                    using var httpClient = new System.Net.Http.HttpClient();
                    httpClient.DefaultRequestHeaders.Add("api-key", brevoApiKey);

                    // Sử dụng BrevoFromEmail hoặc AdminFallbackEmail nếu có, mặc định là fromEmail
                    var brevoSenderEmail = _configuration["BrevoFromEmail"] ?? _configuration["SmtpSettings:AdminFallbackEmail"] ?? fromEmail;

                    var payload = new
                    {
                        sender = new { name = fromName, email = brevoSenderEmail },
                        to = new[] { new { email = toEmail } },
                        subject = subject,
                        htmlContent = htmlBody
                    };

                    var jsonContent = new System.Net.Http.StringContent(
                        System.Text.Json.JsonSerializer.Serialize(payload),
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );

                    var response = await httpClient.PostAsync("https://api.brevo.com/v3/smtp/email", jsonContent);
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[INFO] Email sent successfully via Brevo HTTP API to {toEmail}");
                        return;
                    }

                    var errBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[WARNING] Brevo HTTP API returned status {response.StatusCode}: {errBody}. Falling back.");
                }
                catch (Exception brevoEx)
                {
                    Console.WriteLine($"[WARNING] Brevo HTTP API failed: {brevoEx.Message}. Falling back.");
                }
            }

            // 2. Ưu tiên 2: Gửi qua Resend HTTP API
            if (!string.IsNullOrWhiteSpace(resendApiKey))
            {
                try
                {
                    using var httpClient = new System.Net.Http.HttpClient();
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", resendApiKey);
                    
                    var resendFromEmail = _configuration["ResendFromEmail"] ?? "onboarding@resend.dev";
                    var payload = new
                    {
                        from = $"{fromName} <{resendFromEmail}>",
                        to = new[] { toEmail },
                        subject = subject,
                        html = htmlBody
                    };

                    var jsonContent = new System.Net.Http.StringContent(
                        System.Text.Json.JsonSerializer.Serialize(payload),
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );

                    var response = await httpClient.PostAsync("https://api.resend.com/emails", jsonContent);
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[INFO] Email sent successfully via Resend HTTP API to {toEmail}");
                        return;
                    }

                    var errBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[WARNING] Resend HTTP API returned status {response.StatusCode}: {errBody}. Falling back to SMTP.");
                }
                catch (Exception resendEx)
                {
                    Console.WriteLine($"[WARNING] Resend HTTP API failed: {resendEx.Message}. Falling back to SMTP.");
                }
            }

            // 2. Dự phòng gửi qua SMTP (MailKit)
            var smtp = _configuration.GetSection("SmtpSettings");
            var host = smtp["Host"] ?? "smtp.gmail.com";
            var portStr = smtp["Port"] ?? "587";
            int.TryParse(portStr, out int port);
            if (port <= 0) port = 587;

            var username = (smtp["Username"] ?? "").Trim();
            var password = (smtp["Password"] ?? "").Trim('"').Trim();

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            client.Timeout = 10000;

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
                throw new InvalidOperationException($"Không thể gửi email qua SMTP ({host}:{port}): {ex.Message}. Render.com chặn cổng SMTP 25/465/587. Khuyên dùng Resend HTTP API (thêm ResendApiKey vào Environment Variables).", ex);
            }
        }
    }
}
