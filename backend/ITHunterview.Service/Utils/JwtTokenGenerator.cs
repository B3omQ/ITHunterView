using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ITHunterview.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ITHunterview.Service.Utils
{
    public static class JwtTokenGenerator
    {
        public static string GenerateAccessToken(User user, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["Secret"];
            if (string.IsNullOrEmpty(secretKey))
                throw new InvalidOperationException("JWT Secret is not configured.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Capitalize role names since [Authorize(Roles = "Admin")] is case-sensitive
            var roleName = user.Role?.Name ?? string.Empty;
            var capitalizedRole = string.IsNullOrEmpty(roleName) 
                ? string.Empty 
                : char.ToUpper(roleName[0]) + roleName.Substring(1).ToLower();

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                // Role claim for Authorization policies (original lowercase)
                new Claim(ClaimTypes.Role, roleName.ToLower()),
                // Role claim for Authorization policies (PascalCase)
                new Claim(ClaimTypes.Role, capitalizedRole),
                // Custom claim for easy access
                new Claim("userId", user.Id.ToString())
            };

            if (!int.TryParse(jwtSettings["ExpiryMinutes"], out var expiryMinutes))
            {
                expiryMinutes = 60;
            }

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public static string GenerateSecureToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}
