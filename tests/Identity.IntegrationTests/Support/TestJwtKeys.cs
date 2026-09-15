using System.Security.Cryptography;

namespace Identity.IntegrationTests.Support;

/// <summary>
/// A fresh RSA key pair per test process — never the devenv-committed dev key, so these tests
/// stay self-contained and never accidentally assert against a key that might change or be
/// rotated for unrelated reasons. Kept as a real <see cref="RSA"/> instance for token
/// verification alongside the PEM string Identity's own config needs.
/// </summary>
internal static class TestJwtKeys
{
    public static readonly RSA Key = RSA.Create(2048);

    public static string SigningKeyPem => Key.ExportPkcs8PrivateKeyPem();

    public const string Issuer = "identity-integration-tests";

    public const string Audience = "giftlist";
}
