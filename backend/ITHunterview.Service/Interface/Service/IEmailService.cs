using System.Threading.Tasks;

namespace ITHunterview.Service.Interface.Service
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string toEmail, string verificationToken);
        Task SendPasswordResetEmailAsync(string toEmail, string resetToken);
    }
}
