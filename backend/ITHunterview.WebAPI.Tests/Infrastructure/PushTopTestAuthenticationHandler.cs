using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ITHunterview.WebAPI.Tests.Infrastructure;

public sealed class PushTopTestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "PushTopTestAuth";
    public const string IdentityHeader = "X-Test-Identity";

    public static readonly Guid ValidRecruiterUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ValidCandidateUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public PushTopTestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(IdentityHeader, out var headerValues) ||
            string.IsNullOrWhiteSpace(headerValues.ToString()) ||
            string.Equals(headerValues.ToString(), "anonymous", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identityType = headerValues.ToString().Trim().ToLowerInvariant();
        var claims = new List<Claim>();

        switch (identityType)
        {
            case "candidate":
                claims.Add(new Claim(ClaimTypes.NameIdentifier, ValidCandidateUserId.ToString()));
                claims.Add(new Claim("userId", ValidCandidateUserId.ToString()));
                claims.Add(new Claim(ClaimTypes.Role, "candidate"));
                claims.Add(new Claim("role", "candidate"));
                break;

            case "recruiter":
                claims.Add(new Claim(ClaimTypes.NameIdentifier, ValidRecruiterUserId.ToString()));
                claims.Add(new Claim("userId", ValidRecruiterUserId.ToString()));
                claims.Add(new Claim(ClaimTypes.Role, "recruiter"));
                claims.Add(new Claim("role", "recruiter"));
                break;

            case "recruiter-no-id":
                claims.Add(new Claim(ClaimTypes.Role, "recruiter"));
                claims.Add(new Claim("role", "recruiter"));
                break;

            case "recruiter-invalid-id":
                claims.Add(new Claim(ClaimTypes.NameIdentifier, "not-a-guid"));
                claims.Add(new Claim("userId", "not-a-guid"));
                claims.Add(new Claim(ClaimTypes.Role, "recruiter"));
                claims.Add(new Claim("role", "recruiter"));
                break;

            default:
                return Task.FromResult(AuthenticateResult.Fail($"Unknown test identity: {identityType}"));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
