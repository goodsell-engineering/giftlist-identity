using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.RequestReply;
using BuildingBlocks.Persistence;
using Identity.Contracts.Users;
using Identity.Infrastructure.Platform;
using Identity.IntegrationTests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;

namespace Identity.IntegrationTests.Fixtures;

/// <summary>
/// Builds the real Identity composition root — the exact same
/// <c>AddBuildingBlocksMongo</c>/<c>AddBuildingBlocksRebus</c>/<c>AddIdentityInfrastructure</c>
/// calls <c>Identity.Host</c>'s <c>Program.cs</c> makes — against the containers from
/// <see cref="InfrastructureFixture"/>, plus one "requester" bus standing in for the Gateway, the
/// only other thing that ever sends <see cref="SignUp"/>/<see cref="Login"/> in production. Tests
/// therefore enter through the real Rebus handlers (CONVENTIONS.md "Testing"'s "entered at its real
/// entry point"), never by calling an interactor directly.
/// </summary>
public sealed class IdentityFixture : IAsyncLifetime
{
    private const string DatabaseName = "identity";
    private const string IdentityQueueName = "identity";

    /// <summary>Mirrors the internal <c>UserRepository.CollectionName</c> — not accessible from here, kept in sync by hand.</summary>
    public const string UsersCollectionName = "users";

    private readonly InfrastructureFixture _infrastructure = new();
    private IHost _identityHost = null!;
    private IHost _requesterHost = null!;

    public IMongoDatabase Database { get; private set; } = null!;

    public IBus RequesterBus => _requesterHost.Services.GetRequiredService<IBus>();

    public IRequestReplyBridge RequestReplyBridge => _requesterHost.Services.GetRequiredService<IRequestReplyBridge>();

    public string RabbitMqConnectionString => _infrastructure.RabbitMqConnectionString;

    /// <summary>
    /// GL-62's <c>UserEventPublisherFailureTests</c> needs a real, but disposable, Mongo
    /// connection to stand up a throwaway Identity host next to a throwaway (and, on purpose,
    /// soon-stopped) RabbitMQ container — reusing this shared, healthy Mongo rather than
    /// spinning up a second Mongo container it would never actually write to.
    /// </summary>
    public string MongoConnectionString => _infrastructure.MongoConnectionString;

    /// <summary>
    /// A scope into Identity's own container, for tests that need to call a port directly
    /// (e.g. <c>IUserRepository</c>) rather than through the Rebus handlers — used sparingly,
    /// for the cases the wire-level tests genuinely cannot reach deterministically (GL-57
    /// review: racing eight requests through the bridge cannot tell "the pre-check caught it"
    /// apart from "the index caught it", since both surface the same error code).
    /// </summary>
    public IServiceScope CreateIdentityScope() => _identityHost.Services.CreateScope();

    public async Task InitializeAsync()
    {
        await _infrastructure.InitializeAsync();

        var identityConfig = BuildIdentityConfiguration();
        var identityBuilder = Host.CreateApplicationBuilder();
        identityBuilder.Logging.ClearProviders();
        identityBuilder.Configuration.AddInMemoryCollection(identityConfig);
        identityBuilder.Services.AddBuildingBlocksMongo(identityBuilder.Configuration, DatabaseName);
        identityBuilder.Services.AddBuildingBlocksRebus(identityBuilder.Configuration, IdentityQueueName);
        identityBuilder.Services.AddIdentityInfrastructure(identityBuilder.Configuration);
        _identityHost = identityBuilder.Build();
        await _identityHost.StartAsync();
        await IdentityInfrastructureServiceCollectionExtensions.EnsureIndexesAsync(
            _identityHost.Services, CancellationToken.None);

        Database = _identityHost.Services.GetRequiredService<IMongoDatabase>();

        var requesterBuilder = Host.CreateApplicationBuilder();
        requesterBuilder.Logging.ClearProviders();
        requesterBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [RebusConfigurationExtensions.ConnectionStringConfigKey] = _infrastructure.RabbitMqConnectionString,
        });
        requesterBuilder.Services.AddBuildingBlocksRebus(
            requesterBuilder.Configuration,
            $"identity-tests.{Guid.NewGuid():N}",
            configure: (configurer, _) => configurer.Routing(r => r.TypeBased()
                .Map<SignUp>(IdentityQueueName)
                .Map<Login>(IdentityQueueName)));
        _requesterHost = requesterBuilder.Build();
        await _requesterHost.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _requesterHost.StopAsync();
        _requesterHost.Dispose();
        await _identityHost.StopAsync();
        _identityHost.Dispose();
        await _infrastructure.DisposeAsync();
    }

    /// <summary>
    /// CONVENTIONS.md "Testing": isolate by dropping the database between tests, never by restarting a
    /// container. Re-applies the unique email index afterwards — dropping the database drops it
    /// too, and a test relying on it running right after a reset would otherwise pass for the
    /// wrong reason.
    /// </summary>
    public async Task ResetAsync()
    {
        await Database.Client.DropDatabaseAsync(DatabaseName);
        await IdentityInfrastructureServiceCollectionExtensions.EnsureIndexesAsync(
            _identityHost.Services, CancellationToken.None);
    }

    private Dictionary<string, string?> BuildIdentityConfiguration() => new()
    {
        [RebusConfigurationExtensions.ConnectionStringConfigKey] = _infrastructure.RabbitMqConnectionString,
        [MongoConfigurationExtensions.ConnectionStringConfigKey] = _infrastructure.MongoConnectionString,
        ["Jwt:SigningKeyPem"] = TestJwtKeys.SigningKeyPem,
        ["Jwt:Issuer"] = TestJwtKeys.Issuer,
        ["Jwt:Audience"] = TestJwtKeys.Audience,
    };
}
