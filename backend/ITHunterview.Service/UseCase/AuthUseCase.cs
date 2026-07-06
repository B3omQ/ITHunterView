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
using ITHunterview.Service.Interface.Infrastructure;
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
        private readonly IAuditLogQueue _auditLogQueue;
        private readonly IActorProvider _actorProvider;

        public AuthUseCase(
            IUserRepository userRepository,
            ITokenRepository tokenRepository,
            IRoleRepository roleRepository,
            IEmailVerificationRepository emailVerificationRepository,
            IPasswordResetRepository passwordResetRepository,
            IEmailService emailService,
            IGoogleAuthService googleAuthService,
            IConfiguration configuration,
            IAuditLogQueue auditLogQueue,
            IActorProvider actorProvider)
        {
            _userRepository = userRepository;
            _tokenRepository = tokenRepository;
            _roleRepository = roleRepository;
            _emailVerificationRepository = emailVerificationRepository;
            _passwordResetRepository = passwordResetRepository;
            _emailService = emailService;
            _googleAuthService = googleAuthService;
            _configuration = configuration;
            _auditLogQueue = auditLogQueue;
            _actorProvider = actorProvider;
        }

        // ─── LOGIN ──────────────────────────────────────────────────────────────
        public async Task<ResponseBase<LoginResponseDto>> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepository.GetUserWithRoleByEmailAsync(request.Email);

            if (user == null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                LogFailedLogin(request.Email, "Invalid email or password.");
                return new ResponseBase<LoginResponseDto>("Invalid email or password.");
            }

            if (user.Status == UserStatus.BANNED)
            {
                LogFailedLogin(request.Email, "Your account has been banned.");
                return new ResponseBase<LoginResponseDto>("Your account has been banned.");
            }

            if (user.Status == UserStatus.PENDING_VERIFICATION)
            {
                LogFailedLogin(request.Email, "Please verify your email before logging in.");
                return new ResponseBase<LoginResponseDto>("Please verify your email before logging in.");
            }

            if (user.Status == UserStatus.INACTIVE)
            {
                LogFailedLogin(request.Email, "Account is inactive.");
                return new ResponseBase<LoginResponseDto>("Account is inactive.");
            }

            return await GenerateTokensAndRespond(user);
        }

        // ─── REGISTER ───────────────────────────────────────────────────────────
        public async Task<ResponseBase<LoginResponseDto>> RegisterAsync(RegisterRequestDto request)
        {
            // Validate role
            var allowedRoles = new[] { "candidate", "recruiter" };
            var roleType = request.RoleType.ToLower();
            if (!Array.Exists(allowedRoles, r => r == roleType))
                return new ResponseBase<LoginResponseDto>("Invalid role. Only 'candidate' or 'recruiter' is accepted.");

            // Check duplicate email
            var existing = await _userRepository.GetUserByEmailAsync(request.Email);
            if (existing != null)
            {
                // If account exists but not yet verified → resend verification email
                if (existing.Status == UserStatus.PENDING_VERIFICATION)
                {
                    var newToken = JwtTokenGenerator.GenerateSecureToken();
                    await _emailVerificationRepository.AddTokenAsync(new EmailVerificationTokens
                    {
                        UserId = existing.Id,
                        Token = newToken,
                        ExpiresAt = DateTime.UtcNow.AddHours(24)
                    });
                    try { await _emailService.SendVerificationEmailAsync(existing.Email, newToken); }
                    catch { /* log in production */ }

                    // Return success-like response so frontend redirects to verify-email page
                    return new ResponseBase<LoginResponseDto>
                    {
                        Success = true,
                        Message = "PENDING_VERIFICATION|Account is not verified. We have resent the verification email."
                    };
                }

                // Active or banned account
                return new ResponseBase<LoginResponseDto>("Email is already in use.");
            }

            // Get role
            var role = await _roleRepository.GetByNameAsync(roleType);
            if (role == null)
                return new ResponseBase<LoginResponseDto>("The system has not configured this role. Please contact the administrator.");

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
                "Registration successful! Please check your email to verify your account.");
        }

        // ─── GOOGLE AUTH ─────────────────────────────────────────────────────────
        public async Task<ResponseBase<LoginResponseDto>> GoogleAuthAsync(GoogleAuthRequestDto request)
        {
            // Verify Google token
            var googleUser = await _googleAuthService.VerifyGoogleTokenAsync(request.IdToken);
            if (googleUser == null)
            {
                LogFailedLogin("unknown_google_auth", "Invalid Google token.");
                return new ResponseBase<LoginResponseDto>("Invalid Google token.");
            }

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
                {
                    LogFailedLogin(googleUser.Email, "Google login failed: Role is not configured in the system.");
                    return new ResponseBase<LoginResponseDto>("The system has not configured this role.");
                }

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
            {
                LogFailedLogin(googleUser.Email, "Your account has been banned.");
                return new ResponseBase<LoginResponseDto>("Your account has been banned.");
            }

            return await GenerateTokensAndRespond(user, googleUser.Picture);
        }

        // ─── REFRESH TOKEN ───────────────────────────────────────────────────────
        public async Task<ResponseBase<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            var storedToken = await _tokenRepository.GetRefreshTokenAsync(request.RefreshToken);

            if (storedToken == null)
                return new ResponseBase<LoginResponseDto>("Invalid refresh token.");

            if (storedToken.ExpiresAt < DateTime.UtcNow)
                return new ResponseBase<LoginResponseDto>("Refresh token has expired. Please login again.");

            var user = storedToken.User;

            if (user.Status == UserStatus.BANNED)
                return new ResponseBase<LoginResponseDto>("Your account has been banned.");

            // Revoke old token (rotation)
            await _tokenRepository.RevokeRefreshTokenAsync(request.RefreshToken);

            return await GenerateTokensAndRespond(user);
        }

        // ─── LOGOUT ─────────────────────────────────────────────────────────────
        public async Task<ResponseBase> LogoutAsync(LogoutRequestDto request)
        {
            await _tokenRepository.RevokeRefreshTokenAsync(request.RefreshToken);
            return ResponseBase.Ok("Logged out successfully.");
        }

        // ─── CHANGE PASSWORD ─────────────────────────────────────────────────────
        public async Task<ResponseBase> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                return ResponseBase.Fail("User does not exist.");

            if (!PasswordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                return ResponseBase.Fail("Current password is incorrect.");

            user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateUserAsync(user);

            // Revoke all refresh tokens for security
            await _tokenRepository.RevokeAllUserRefreshTokensAsync(userId);

            return ResponseBase.Ok("Changed password successfully. Please login again.");
        }

        // ─── FORGOT PASSWORD ─────────────────────────────────────────────────────
        public async Task<ResponseBase> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            var user = await _userRepository.GetUserByEmailAsync(request.Email);

            // Always return success to prevent email enumeration
            if (user == null)
                return ResponseBase.Ok("If the email exists, we have sent instructions to reset your password.");

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

            return ResponseBase.Ok("If the email exists, we have sent instructions to reset your password.");
        }

        // ─── RESET PASSWORD ──────────────────────────────────────────────────────
        public async Task<ResponseBase> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            var resetRecord = await _passwordResetRepository.GetByTokenAndEmailAsync(request.Token, request.Email);

            if (resetRecord == null)
                return ResponseBase.Fail("Invalid or used token.");

            if (resetRecord.ExpiresAt < DateTime.UtcNow)
                return ResponseBase.Fail("Token has expired. Please request a new password reset.");

            var user = resetRecord.User;
            user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateUserAsync(user);

            await _passwordResetRepository.MarkUsedAsync(resetRecord.Id);
            await _tokenRepository.RevokeAllUserRefreshTokensAsync(user.Id);

            return ResponseBase.Ok("Reset password successfully. Please login again.");
        }

        // ─── VERIFY EMAIL ────────────────────────────────────────────────────────
        public async Task<ResponseBase> VerifyEmailAsync(string token)
        {
            var verifyRecord = await _emailVerificationRepository.GetByTokenAsync(token);

            if (verifyRecord == null)
                return ResponseBase.Fail("Invalid or used verification token.");

            if (verifyRecord.ExpiresAt < DateTime.UtcNow)
                return ResponseBase.Fail("Verification token has expired. Please register again.");

            var user = verifyRecord.User;
            user.Status = UserStatus.ACTIVE;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateUserAsync(user);

            await _emailVerificationRepository.MarkUsedAsync(verifyRecord.Id);

            return ResponseBase.Ok("Email verified successfully! You can login now.");
        }

        // ─── RESEND VERIFICATION EMAIL ───────────────────────────────────────────
        public async Task<ResponseBase> ResendVerificationEmailAsync(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);

            // Always return OK to prevent email enumeration
            if (user == null || user.Status == UserStatus.ACTIVE)
                return ResponseBase.Ok("If the email exists and is not verified, we have resent the verification link.");

            // Invalidate old tokens by creating a new one (old ones will expire naturally)
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
                // Don't fail if email sending fails; log in production
            }

            return ResponseBase.Ok("If the email exists and is not verified, we have resent the verification link.");
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

            // Record successful login activity
            try
            {
                var log = UserActivityLogs.Create(
                    userId: user.Id,
                    actorRole: user.Role?.Name ?? "anonymous",
                    category: ActivityLogCategory.AUTH,
                    actorEmail: user.Email,
                    action: "Login successfully.",
                    status: ActivityLogStatus.SUCCESS,
                    ipAddress: _actorProvider.IpAddress,
                    userAgent: _actorProvider.UserAgent
                );

                _auditLogQueue.TryEnqueue(log);
            }
            catch
            {
                // Avoid interrupting the token response flow if logging fails
            }

            return new ResponseBase<LoginResponseDto>(response, "Logged in successfully.");
        }

        private void LogFailedLogin(string email, string justification)
        {
            try
            {
                var log = UserActivityLogs.Create(
                    userId: null,
                    actorRole: "anonymous",
                    category: ActivityLogCategory.SECURITY,
                    actorEmail: email,
                    action: $"Login failed: {justification}",
                    status: ActivityLogStatus.FAIL,
                    ipAddress: _actorProvider.IpAddress,
                    userAgent: _actorProvider.UserAgent
                );

                _auditLogQueue.TryEnqueue(log);
            }
            catch
            {
                // Avoid failures affecting the main business flow
            }
        }
    }
}
