using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Auth;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Utils;
using Microsoft.Extensions.Configuration;

namespace ITHunterview.Service.UseCase
{
    public class AuthUseCase : IAuthUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenRepository _tokenRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IEmailVerificationRepository _emailVerificationRepository;
        private readonly IPasswordResetRepository _passwordResetRepository;
        private readonly IEmailService _emailService;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IConfiguration _configuration;

        public AuthUseCase(
            IUserRepository userRepository,
            ITokenRepository tokenRepository,
            IRoleRepository roleRepository,
            IEmailVerificationRepository emailVerificationRepository,
            IPasswordResetRepository passwordResetRepository,
            IEmailService emailService,
            IGoogleAuthService googleAuthService,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _tokenRepository = tokenRepository;
            _roleRepository = roleRepository;
            _emailVerificationRepository = emailVerificationRepository;
            _passwordResetRepository = passwordResetRepository;
            _emailService = emailService;
            _googleAuthService = googleAuthService;
            _configuration = configuration;
        }

        // ─── LOGIN ──────────────────────────────────────────────────────────────
        public async Task<ResponseBase<LoginResponseDto>> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepository.GetUserWithRoleByEmailAsync(request.Email);

            if (user == null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
                return new ResponseBase<LoginResponseDto>("Email hoặc mật khẩu không đúng.");

            if (user.Status == UserStatus.BANNED)
                return new ResponseBase<LoginResponseDto>("Tài khoản của bạn đã bị khóa.");

            if (user.Status == UserStatus.PENDING_VERIFICATION)
                return new ResponseBase<LoginResponseDto>("Vui lòng xác thực email trước khi đăng nhập.");

            if (user.Status == UserStatus.INACTIVE)
                return new ResponseBase<LoginResponseDto>("Tài khoản không còn hoạt động.");

            return await GenerateTokensAndRespond(user);
        }

        // ─── REGISTER ───────────────────────────────────────────────────────────
        public async Task<ResponseBase<LoginResponseDto>> RegisterAsync(RegisterRequestDto request)
        {
            // Validate role
            var allowedRoles = new[] { "candidate", "recruiter" };
            var roleType = request.RoleType.ToLower();
            if (!Array.Exists(allowedRoles, r => r == roleType))
                return new ResponseBase<LoginResponseDto>("Role không hợp lệ. Chỉ chấp nhận 'candidate' hoặc 'recruiter'.");

            // Check duplicate email
            var existing = await _userRepository.GetUserByEmailAsync(request.Email);
            if (existing != null)
                return new ResponseBase<LoginResponseDto>("Email này đã được sử dụng.");

            // Get role
            var role = await _roleRepository.GetByNameAsync(roleType);
            if (role == null)
                return new ResponseBase<LoginResponseDto>("Hệ thống chưa cấu hình role. Vui lòng liên hệ quản trị viên.");

            // Create user
            var user = new User
            {
                Email = request.Email,
                PasswordHash = PasswordHasher.HashPassword(request.Password),
                RoleId = role.Id,
                Status = UserStatus.PENDING_VERIFICATION,
                CreatedAt = DateTime.UtcNow
            };

            // Create profile
            if (roleType == "candidate")
            {
                user.CandidateProfile = new CandidateProfiles
                {
                    IsVisibleToRecruiters = true
                };
            }
            else if (roleType == "recruiter")
            {
                user.RecruiterProfile = new RecruiterProfiles();
            }

            await _userRepository.AddUserAsync(user);

            // Send verification email
            var verifyToken = JwtTokenGenerator.GenerateSecureToken();
            await _emailVerificationRepository.AddTokenAsync(new EmailVerificationTokens
            {
                UserId = user.Id,
                Token = verifyToken,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            });

            try
            {
                await _emailService.SendVerificationEmailAsync(user.Email, verifyToken);
            }
            catch
            {
                // Don't fail registration if email fails; log in production
            }

            return new ResponseBase<LoginResponseDto>(
                null!,
                "Đăng ký thành công! Vui lòng kiểm tra email để xác thực tài khoản.");
        }

        // ─── GOOGLE AUTH ─────────────────────────────────────────────────────────
        public async Task<ResponseBase<LoginResponseDto>> GoogleAuthAsync(GoogleAuthRequestDto request)
        {
            // Verify Google token
            var googleUser = await _googleAuthService.VerifyGoogleTokenAsync(request.IdToken);
            if (googleUser == null)
                return new ResponseBase<LoginResponseDto>("Google token không hợp lệ.");

            // Try to find existing user
            var user = await _userRepository.GetUserWithRoleByEmailAsync(googleUser.Email);

            if (user == null)
            {
                // Auto-register new user
                var roleType = request.RoleType.ToLower();
                if (roleType != "candidate" && roleType != "recruiter")
                    roleType = "candidate";

                var role = await _roleRepository.GetByNameAsync(roleType);
                if (role == null)
                    return new ResponseBase<LoginResponseDto>("Hệ thống chưa cấu hình role.");

                user = new User
                {
                    Email = googleUser.Email,
                    PasswordHash = PasswordHasher.HashPassword(Guid.NewGuid().ToString()), // random password
                    RoleId = role.Id,
                    Status = UserStatus.ACTIVE, // Google accounts are pre-verified
                    CreatedAt = DateTime.UtcNow
                };

                if (roleType == "candidate")
                {
                    user.CandidateProfile = new CandidateProfiles
                    {
                        IsVisibleToRecruiters = true
                    };
                }
                else if (roleType == "recruiter")
                {
                    user.RecruiterProfile = new RecruiterProfiles();
                }

                await _userRepository.AddUserAsync(user);

                // Reload with role
                user = await _userRepository.GetUserWithRoleAsync(user.Id);
            }

            if (user!.Status == UserStatus.BANNED)
                return new ResponseBase<LoginResponseDto>("Tài khoản của bạn đã bị khóa.");

            return await GenerateTokensAndRespond(user, googleUser.Picture);
        }

        // ─── REFRESH TOKEN ───────────────────────────────────────────────────────
        public async Task<ResponseBase<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            var storedToken = await _tokenRepository.GetRefreshTokenAsync(request.RefreshToken);

            if (storedToken == null)
                return new ResponseBase<LoginResponseDto>("Refresh token không hợp lệ.");

            if (storedToken.ExpiresAt < DateTime.UtcNow)
                return new ResponseBase<LoginResponseDto>("Refresh token đã hết hạn. Vui lòng đăng nhập lại.");

            var user = storedToken.User;

            if (user.Status == UserStatus.BANNED)
                return new ResponseBase<LoginResponseDto>("Tài khoản của bạn đã bị khóa.");

            // Revoke old token (rotation)
            await _tokenRepository.RevokeRefreshTokenAsync(request.RefreshToken);

            return await GenerateTokensAndRespond(user);
        }

        // ─── LOGOUT ─────────────────────────────────────────────────────────────
        public async Task<ResponseBase> LogoutAsync(LogoutRequestDto request)
        {
            await _tokenRepository.RevokeRefreshTokenAsync(request.RefreshToken);
            return ResponseBase.Ok("Đăng xuất thành công.");
        }

        // ─── CHANGE PASSWORD ─────────────────────────────────────────────────────
        public async Task<ResponseBase> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                return ResponseBase.Fail("Người dùng không tồn tại.");

            if (!PasswordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                return ResponseBase.Fail("Mật khẩu hiện tại không đúng.");

            user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateUserAsync(user);

            // Revoke all refresh tokens for security
            await _tokenRepository.RevokeAllUserRefreshTokensAsync(userId);

            return ResponseBase.Ok("Đổi mật khẩu thành công. Vui lòng đăng nhập lại.");
        }

        // ─── FORGOT PASSWORD ─────────────────────────────────────────────────────
        public async Task<ResponseBase> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            var user = await _userRepository.GetUserByEmailAsync(request.Email);

            // Always return success to prevent email enumeration
            if (user == null)
                return ResponseBase.Ok("Nếu email tồn tại, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu.");

            var resetToken = JwtTokenGenerator.GenerateSecureToken();
            await _passwordResetRepository.AddTokenAsync(new PasswordResets
            {
                UserId = user.Id,
                Token = resetToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            });

            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken);
            }
            catch
            {
                // Log in production
            }

            return ResponseBase.Ok("Nếu email tồn tại, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu.");
        }

        // ─── RESET PASSWORD ──────────────────────────────────────────────────────
        public async Task<ResponseBase> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            var resetRecord = await _passwordResetRepository.GetByTokenAndEmailAsync(request.Token, request.Email);

            if (resetRecord == null)
                return ResponseBase.Fail("Token không hợp lệ hoặc đã được sử dụng.");

            if (resetRecord.ExpiresAt < DateTime.UtcNow)
                return ResponseBase.Fail("Token đã hết hạn. Vui lòng yêu cầu đặt lại mật khẩu mới.");

            var user = resetRecord.User;
            user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateUserAsync(user);

            await _passwordResetRepository.MarkUsedAsync(resetRecord.Id);
            await _tokenRepository.RevokeAllUserRefreshTokensAsync(user.Id);

            return ResponseBase.Ok("Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại.");
        }

        // ─── VERIFY EMAIL ────────────────────────────────────────────────────────
        public async Task<ResponseBase> VerifyEmailAsync(string token)
        {
            var verifyRecord = await _emailVerificationRepository.GetByTokenAsync(token);

            if (verifyRecord == null)
                return ResponseBase.Fail("Token xác thực không hợp lệ hoặc đã được sử dụng.");

            if (verifyRecord.ExpiresAt < DateTime.UtcNow)
                return ResponseBase.Fail("Token xác thực đã hết hạn. Vui lòng đăng ký lại.");

            var user = verifyRecord.User;
            user.Status = UserStatus.ACTIVE;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateUserAsync(user);

            await _emailVerificationRepository.MarkUsedAsync(verifyRecord.Id);

            return ResponseBase.Ok("Xác thực email thành công! Bạn có thể đăng nhập ngay bây giờ.");
        }

        // ─── PRIVATE HELPER ──────────────────────────────────────────────────────
        private async Task<ResponseBase<LoginResponseDto>> GenerateTokensAndRespond(
            User user, string? avatarUrlOverride = null)
        {
            var accessToken = JwtTokenGenerator.GenerateAccessToken(user, _configuration);
            var refreshTokenString = JwtTokenGenerator.GenerateRefreshToken();

            await _tokenRepository.AddRefreshTokenAsync(new RefreshToken
            {
                Token = refreshTokenString,
                IsRevoked = false,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            // Resolve full name & avatar from profile
            string fullName = user.Email;
            string? avatarUrl = avatarUrlOverride;

            if (user.CandidateProfile != null)
            {
                fullName = $"{user.CandidateProfile.FirstName} {user.CandidateProfile.LastName}".Trim();
                if (string.IsNullOrEmpty(fullName)) fullName = user.Email;
                avatarUrl ??= user.CandidateProfile.AvatarUrl;
            }
            else if (user.RecruiterProfile != null)
            {
                fullName = user.RecruiterProfile.FullName ?? user.Email;
                avatarUrl ??= user.RecruiterProfile.AvatarUrl;
            }

            var response = new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenString,
                UserId = user.Id,
                Email = user.Email,
                FullName = fullName,
                Role = user.Role?.Name ?? string.Empty,
                AvatarUrl = avatarUrl
            };

            return new ResponseBase<LoginResponseDto>(response, "Đăng nhập thành công.");
        }
    }
}
