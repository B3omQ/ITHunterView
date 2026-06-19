using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Auth;
using ITHunterview.Service.DTOs.Common;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface IAuthUseCase
    {
        Task<ResponseBase<LoginResponseDto>> LoginAsync(LoginRequestDto request);
        Task<ResponseBase<LoginResponseDto>> RegisterAsync(RegisterRequestDto request);
        Task<ResponseBase<LoginResponseDto>> GoogleAuthAsync(GoogleAuthRequestDto request);
        Task<ResponseBase<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request);
        Task<ResponseBase> LogoutAsync(LogoutRequestDto request);
        Task<ResponseBase> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request);
        Task<ResponseBase> ForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task<ResponseBase> ResetPasswordAsync(ResetPasswordRequestDto request);
        Task<ResponseBase> VerifyEmailAsync(string token);
    }
}
