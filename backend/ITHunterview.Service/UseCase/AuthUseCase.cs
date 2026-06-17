using System;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Auth;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Utils;
using Microsoft.Extensions.Configuration;

namespace ITHunterview.Service.UseCase
{
    public class AuthUseCase : IAuthUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenRepository _tokenRepository;
        private readonly IConfiguration _configuration;

        public AuthUseCase(IUserRepository userRepository, ITokenRepository tokenRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _tokenRepository = tokenRepository;
            _configuration = configuration;
        }

        public async Task<ResponseBase<LoginResponseDto>> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepository.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return new ResponseBase<LoginResponseDto>("Invalid email or password.");
            }

            var isPasswordValid = PasswordHasher.VerifyPassword(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return new ResponseBase<LoginResponseDto>("Invalid email or password.");
            }

            var accessToken = JwtTokenGenerator.GenerateAccessToken(user, _configuration);
            var refreshTokenString = JwtTokenGenerator.GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                Token = refreshTokenString,
                JwtId = Guid.NewGuid().ToString(),
                IsUsed = false,
                IsRevoked = false,
                UserId = user.Id,
                ExpiryDate = DateTime.UtcNow.AddDays(7)
            };

            await _tokenRepository.AddRefreshTokenAsync(refreshToken);

            var response = new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenString,
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName
            };

            return new ResponseBase<LoginResponseDto>(response);
        }
    }
}
