using Identity.Application.Common;
using Identity.Application.Users;
using Identity.Application.Users.Login;
using Identity.Application.Users.SignUp;
using Identity.Infrastructure.Platform.Security;
using Identity.Infrastructure.Users.Messaging;
using Identity.Infrastructure.Users.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Rebus.Config;

namespace Identity.Infrastructure.Platform;

/// <summary>
/// Identity's composition root, called once from <c>Identity.Host</c>'s <c>Program.cs</c>. Host
/// itself contains no wiring beyond the call to this method (CONVENTIONS.md "Project reference graph"); everything below
/// is grouped by domain (<c>Users/...</c>) rather than by technical category, same as production
/// code (CONVENTIONS.md "Folder structure") — <c>Platform/</c> holds only this aggregator, which belongs to no
/// single domain.
/// </summary>
public static class IdentityInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddUsers(services, configuration);
        return services;
    }

    /// <summary>
    /// Applies startup-time infrastructure that needs a live connection — today, just the unique
    /// email index (ARCHITECTURE.md "Data model"). Called once from <c>Program.cs</c> after the host is
    /// built, mirroring how Mongo/Rebus health checks are wired ahead of any use case.
    /// </summary>
    public static Task EnsureIndexesAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var database = serviceProvider.GetRequiredService<IMongoDatabase>();
        return UserRepository.EnsureIndexesAsync(database, cancellationToken);
    }

    private static void AddUsers(IServiceCollection services, IConfiguration configuration)
    {
        RequireJwtConfiguration(configuration);
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.ConfigurationSection));

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDomainEventPublisher, UserEventPublisher>();

        services.AddScoped<IValidator<SignUpRequest>, SignUpValidator>();
        services.AddScoped<IInteractor<SignUpRequest, SignUpResponse>, SignUpInteractor>();

        services.AddScoped<IValidator<LoginRequest>, LoginValidator>();
        services.AddScoped<IInteractor<LoginRequest, LoginResponse>, LoginInteractor>();

        // One open-generic decorator pair, applied to every IInteractor<,> registered above,
        // rather than a hand-written decorator per use case — see
        // Identity.Application.Common.IInteractor's doc comment for why ISignUp/ILogin
        // themselves cannot be the decoration target. Validation, then Logging, in that order in
        // every service (CONVENTIONS.md "Use cases") — Logging is therefore the outermost decorator and
        // also observes a validation failure, not just a business one.
        services.Decorate(typeof(IInteractor<,>), typeof(Validating<,>));
        services.Decorate(typeof(IInteractor<,>), typeof(Logging<,>));

        services.AddRebusHandler<SignUpHandler>();
        services.AddRebusHandler<LoginHandler>();
    }

    /// <summary>
    /// Same fail-fast-with-an-actionable-message shape as AddBuildingBlocksMongo/Rebus, extended
    /// to all three required <see cref="JwtOptions"/> members — options binding does not enforce
    /// `required`, so a missing <c>Jwt:Issuer</c>/<c>Jwt:Audience</c> would otherwise silently
    /// mint tokens with no `iss`/`aud` claim and the Gateway would reject them with no useful
    /// diagnostic, instead of failing here at startup.
    /// </summary>
    private static void RequireJwtConfiguration(IConfiguration configuration)
    {
        RequireJwtValue(configuration, "SigningKeyPem", "a PKCS8 PEM-encoded RSA private key");
        RequireJwtValue(configuration, "Issuer", "the token issuer");
        RequireJwtValue(configuration, "Audience", "the token audience");
    }

    private static void RequireJwtValue(IConfiguration configuration, string key, string description)
    {
        var value = configuration[$"{JwtOptions.ConfigurationSection}:{key}"];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Missing required configuration value '{JwtOptions.ConfigurationSection}:{key}' " +
                $"({description}) — set via the 'Jwt__{key}' environment variable. Identity is the " +
                "only service that holds the private key (ARCHITECTURE.md \"Why workers still need a little HTTP\"); the matching " +
                "public key is provisioned to the Gateway separately.");
        }
    }
}
