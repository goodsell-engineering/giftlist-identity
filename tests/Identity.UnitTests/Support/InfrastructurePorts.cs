using System.Security.Cryptography;
using Identity.Application.Common;
using Identity.Infrastructure.Platform;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.UnitTests.Support;

/// <summary>
/// Resolves <see cref="IPasswordHasher"/> through the same public
/// <c>AddIdentityInfrastructure</c> composition root <c>Identity.Host</c> calls, rather than
/// reaching for its internal implementation type directly — no new InternalsVisibleTo grant
/// needed for a test that only cares about the port's contract. Neither this port nor Mongo/Rebus
/// is touched, so nothing here needs a container.
/// </summary>
/// <remarks>
/// JWT claims/issuer/audience/expiry are deliberately <b>not</b> resolved or asserted here —
/// Identity.IntegrationTests' SignUp test already proves them end to end under a real RS256 key
/// (CONVENTIONS.md "Testing": don't unit-test what the integration suite already covers). The RSA key
/// below exists only to satisfy <c>AddIdentityInfrastructure</c>'s fail-fast Jwt config check, so
/// the composition root can be called at all to reach <see cref="IPasswordHasher"/>.
/// </remarks>
internal static class InfrastructurePorts
{
    public static IPasswordHasher BuildPasswordHasher()
    {
        using var placeholderSigningKey = RSA.Create(2048);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKeyPem"] = placeholderSigningKey.ExportPkcs8PrivateKeyPem(),
                ["Jwt:Issuer"] = "identity-unit-tests",
                ["Jwt:Audience"] = "giftlist",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddIdentityInfrastructure(configuration);
        var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IPasswordHasher>();
    }
}
