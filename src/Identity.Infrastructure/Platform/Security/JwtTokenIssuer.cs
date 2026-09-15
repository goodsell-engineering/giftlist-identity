using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Identity.Application.Common;
using Identity.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure.Platform.Security;

/// <summary>
/// Signs access tokens with an asymmetric (RSA) key (ARCHITECTURE.md "Why workers still need a little HTTP"): Identity holds the
/// private key and remains the only service that can mint a token; the Gateway validates with
/// the corresponding public key, mounted into it as config. No key-rotation story is needed at
/// this scale — one static key pair for the demo's lifetime.
/// </summary>
internal sealed class JwtTokenIssuer : ITokenIssuer, IDisposable
{
    private readonly JwtOptions _options;
    private readonly RSA _rsa;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenIssuer(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _rsa = RSA.Create();

        // devenv's .env.example carries the dev key as a single-line value with literal '\n'
        // escapes (the usual trick for a multi-line PEM in a KEY=VALUE env file) — undo that
        // before handing it to the BCL parser, which wants real line breaks.
        _rsa.ImportFromPem(_options.SigningKeyPem.Replace("\\n", "\n", StringComparison.Ordinal));
        _signingCredentials = new SigningCredentials(new RsaSecurityKey(_rsa), SecurityAlgorithms.RsaSha256);
    }

    public AccessToken Issue(UserId subjectId, Email email, DisplayName displayName)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(_options.AccessTokenLifetime);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, subjectId.Value.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email.Value),
            new Claim("name", displayName.Value),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: _signingCredentials);

        var value = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessToken(value, new DateTimeOffset(expiresAt, TimeSpan.Zero));
    }

    public void Dispose() => _rsa.Dispose();
}
