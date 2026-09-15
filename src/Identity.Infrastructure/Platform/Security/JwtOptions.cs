namespace Identity.Infrastructure.Platform.Security;

/// <summary>
/// Bound from the <c>Jwt</c> configuration section. <see cref="SigningKeyPem"/> is the asymmetric
/// private key (ARCHITECTURE.md "Why workers still need a little HTTP") — Identity is the only service that ever sees it. The
/// corresponding public key is provisioned to the Gateway separately, as config; that is a
/// Gateway-side concern and out of this service's scope.
/// </summary>
public sealed class JwtOptions
{
    public const string ConfigurationSection = "Jwt";

    /// <summary>PKCS8 PEM-encoded RSA private key.</summary>
    public required string SigningKeyPem { get; init; }

    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);
}
