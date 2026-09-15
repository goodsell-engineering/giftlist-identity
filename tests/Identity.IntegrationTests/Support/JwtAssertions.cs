using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Identity.IntegrationTests.Support;

/// <summary>Validates an access token the same way the Gateway would — under the public half of the RS256 key pair.</summary>
internal static class JwtAssertions
{
    public static (ClaimsPrincipal Principal, JwtSecurityToken Token) Validate(string accessToken)
    {
        // Without this, JwtSecurityTokenHandler silently remaps short claim types ("sub",
        // "email") onto long legacy XML-namespace URIs on the way into the ClaimsPrincipal —
        // fine for ASP.NET's own [Authorize] pipeline, but it would make this assertion check
        // the wrong claim type name rather than what JwtTokenIssuer actually put on the wire.
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = TestJwtKeys.Issuer,
            ValidateAudience = true,
            ValidAudience = TestJwtKeys.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(TestJwtKeys.Key),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
        };

        var principal = handler.ValidateToken(accessToken, parameters, out var validatedToken);
        return (principal, (JwtSecurityToken)validatedToken);
    }
}
