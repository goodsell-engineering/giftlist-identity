using BuildingBlocks.Messaging;
using BuildingBlocks.Persistence;
using Identity.Application.Common;
using Identity.Domain.Users;
using Identity.Domain.Users.Events;
using Identity.Infrastructure.Platform;
using Identity.IntegrationTests.Fixtures;
using Identity.IntegrationTests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rebus.Config;
using Testcontainers.RabbitMq;

namespace Identity.IntegrationTests.Users;

/// <summary>
/// GL-62: Identity used to let a publish failure propagate out of
/// <c>UserEventPublisher.PublishAsync</c>; GiftLists swallowed and logged Critical instead.
/// Ryan's decision was swallow-and-log for both — neither behaviour closes the save-then-publish
/// dual-write gap (that needs event sourcing, out of scope for this demo), and propagating gains
/// nothing here: four of GiftLists' five interactors return before publish on a re-run, so Rebus
/// redelivery cannot usefully republish a lost event, meaning propagating would both lose the
/// event AND remove the loud log. This proves the fix against a real, dead broker rather than a
/// mock that throws — a mock would only prove the mock throws.
/// </summary>
/// <remarks>
/// Asserts BOTH no-throw AND the Critical log entry, not no-throw alone. A log-only assertion is
/// satisfiable under either swallow or propagate and pins nothing on its own — but no-throw
/// alone has exactly the opposite gap: it stays green if <c>LogCritical</c> were deleted from
/// the catch block outright, which is a *silent* swallow, the one behaviour Ryan's decision does
/// not authorise (the log is the explicit justification for swallowing at all). GiftLists'
/// <c>GiftListEventPublisherTests</c> hit this exact gap first (Batch 12 review) and this test
/// mirrors its fix via the same ported <see cref="LogCapture"/> rather than re-deriving it.
/// </remarks>
/// <remarks>
/// Deliberately its own throwaway RabbitMQ container and its own throwaway Identity host,
/// rather than reaching into <see cref="IdentityFixture"/>'s shared one: stopping the shared
/// broker to prove this would also break every other test in the collection for the rest of the
/// run (confirmed empirically — Testcontainers reassigns the host port on restart, so anything
/// that cached the original connection string, i.e. every other host built by
/// <see cref="IdentityFixture"/>, is left pointing at a dead port with no way to recover short of
/// tearing the whole fixture down). A dedicated container that this class owns end-to-end can be
/// killed for real with no blast radius. Mongo is the one exception — it reuses
/// <see cref="IdentityFixture"/>'s real, healthy instance, because <c>AddIdentityInfrastructure</c>
/// needs an <c>IMongoDatabase</c> to build, even though this test never writes to it.
/// </remarks>
[Collection(IdentityCollection.Name)]
public sealed class UserEventPublisherFailureTests(IdentityFixture fixture)
{
    [Fact]
    public async Task PublishAsync_ShouldNotThrow_WhenTheBrokerIsGenuinelyUnavailable()
    {
        // Arrange — a real broker, started normally so the host's Rebus bus connects to it just
        // like production, then killed outright so the next publish attempt fails for the same
        // reason it would in an outage: the connection is gone, not mocked.
        var deadBroker = new RabbitMqBuilder("rabbitmq:3.13-management")
            // Labelled because GL-92's rule is that every container this suite starts is
            // attributable to it in `docker ps`, with no "this one cannot collide" exception for a
            // reader to have to re-derive.
            //
            // THIS CONTAINER MUST NEVER GAIN .WithReuse. The label does not protect it — it makes
            // the danger worse, and hides it from the architecture rule. Labelled "identity" and
            // reused, its configuration is identical to InfrastructureFixture's broker, so it
            // would attach to the live suite broker every other test in this collection is using,
            // and the StopAsync below would then kill it mid-run. The rule cannot catch that: the
            // label is correct. Disposal below is this container's whole lifecycle, deliberately.
            //
            // It also carries no .WithReliableWaitStrategy() (GL-93), and that is the same
            // decision, not a second oversight: GL-93's defect is a wait strategy corrupted by a
            // container's history across runs, and this container has no history — it is started
            // once, killed once, by this test alone. SuiteLabelRuleTests names this file as the
            // one deliberate exception to "every container calls .WithReliableWaitStrategy()", so
            // that exception lives in the rule a future change would have to convince, not only
            // in this comment.
            .WithLabel("giftlist.suite", InfrastructureFixture.SuiteLabel)
            .Build();
        await deadBroker.StartAsync();
        var logs = new LogCapture();

        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        // Kept after ClearProviders so the only provider is this one (mirrors GiftListsFixture) —
        // test output stays quiet while assertions about what was logged remain possible.
        builder.Logging.AddProvider(logs);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [RebusConfigurationExtensions.ConnectionStringConfigKey] = deadBroker.GetConnectionString(),
            [MongoConfigurationExtensions.ConnectionStringConfigKey] = fixture.MongoConnectionString,
            ["Jwt:SigningKeyPem"] = TestJwtKeys.SigningKeyPem,
            ["Jwt:Issuer"] = TestJwtKeys.Issuer,
            ["Jwt:Audience"] = TestJwtKeys.Audience,
        });
        builder.Services.AddBuildingBlocksMongo(builder.Configuration, "identity");
        builder.Services.AddBuildingBlocksRebus(
            builder.Configuration, $"identity-tests-gl62.{Guid.NewGuid():N}");
        builder.Services.AddIdentityInfrastructure(builder.Configuration);
        var host = builder.Build();
        await host.StartAsync();

        try
        {
            using var scope = host.Services.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();
            var domainEvent = new UserRegistered(
                UserId.New(),
                new Email($"gl62-{Guid.NewGuid():N}@example.com"),
                new DisplayName("GL-62 Regression"),
                DateTimeOffset.UtcNow);

            await deadBroker.StopAsync();

            // Act
            var exception = await Record.ExceptionAsync(
                () => publisher.PublishAsync([domainEvent], CancellationToken.None));

            // Assert
            Assert.Null(exception);

            // ...and that it actually REPORTED. Asserting only "did not throw" left this green if
            // the LogCritical call were deleted entirely — a silent swallow, which is not what
            // Ryan's decision authorised (GiftLists' Batch 12 review found the same gap first).
            var critical = Assert.Single(logs.Entries, e => e.Level == LogLevel.Critical);
            Assert.Contains(nameof(UserRegistered), critical.Message, StringComparison.Ordinal);
        }
        finally
        {
            await host.StopAsync();
            host.Dispose();
            await deadBroker.DisposeAsync();
        }
    }
}
