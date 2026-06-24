using System;
using System.Threading.Tasks;
using Google.Apis.Auth;
using ITHunterview.Service.Interface.Service;
using Microsoft.Extensions.Configuration;

namespace ITHunterview.Service.Service
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly string _clientId;

        public GoogleAuthService(IConfiguration configuration)
        {
            _clientId = configuration["GoogleAuth:ClientId"] ?? string.Empty;
        }

        public async Task<GoogleUserInfo?> VerifyGoogleTokenAsync(string idToken)
        {
            if (string.IsNullOrEmpty(_clientId))
            {
                throw new InvalidOperationException("GoogleAuth:ClientId is not configured.");
            }
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _clientId }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

                return new GoogleUserInfo
                {
                    GoogleId = payload.Subject,
                    Email = payload.Email,
                    Name = payload.Name,
                    Picture = payload.Picture
                };
            }
            catch (InvalidJwtException)
            {
                return null;
            }
        }
    }
}
