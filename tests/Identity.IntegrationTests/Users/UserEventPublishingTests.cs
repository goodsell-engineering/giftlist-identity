using BuildingBlocks.Messaging;
using BuildingBlocks.Testing;
using Identity.Contracts.Users;
using Identity.Contracts.Users.Events;
using Identity.IntegrationTests.Fixtures;
using Identity.IntegrationTests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rebus.Bus;
using Rebus.Config;

namespace Identity.IntegrationTests.Users;

/// <summary>
/// <see cref="UserRegisteredV1"/> landing on a real broker under its expected type name — the
/// only way to prove Rebus's own type-name-based routing/deserialization (CONVENTIONS.md "Testing")
/// still lines up with what a subscriber (the Gateway, in production) actually asks to subscribe
/// to. An in-memory transport would hand the object straight to a handler and never touch this.
/// </summary>
[Collection(IdentityCollection.Name)]
public sealed class UserEventPublishingTests(IdentityFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SignUp_ShouldPublishUserRegisteredV1_UnderItsOwnTypeName_ToARealSubscriber()
    {
        // Arrange — a subscriber wired up exactly the way the Gateway would be: it asks Rebus to
        // subscribe to UserRegisteredV1 by .NET type, and only receives anything at all if the
        // publisher's wire type name still matches what the subscription was registered under.
        var (subscriber, queueName, capture) = await StartSubscriberAsync();
        var email = $"published-{Guid.NewGuid():N}@example.com";

        try
        {
            // Act
            var signedUp = await fixture.RequestReplyBridge.SendAndAwaitReply<SignUpReply>(
                new SignUp(email, "correct horse battery staple", "Ada Lovelace"));
            Assert.True(signedUp.IsSuccess);
            var (received, typeHeader) = await capture.Completion.Task.WaitAsync(TimeSpan.FromSeconds(15));

            // Assert
            Assert.Equal(signedUp.Value.UserId, received.UserId);
            Assert.Equal(email, received.Email);
            Assert.Equal("Ada Lovelace", received.DisplayName);
            Assert.NotNull(typeHeader);
            Assert.Contains(nameof(UserRegisteredV1), typeHeader, StringComparison.Ordinal);
        }
        finally
        {
            // GL-57 review: on a .WithReuse(true) container, a queue and its topic binding left
            // behind here would accumulate across every local run and keep routing future
            // UserRegisteredV1 events into a queue nobody drains — mirrors
            // BuildingBlocks.IntegrationTests' TestRebusHost.DisposeAsync.
            await subscriber.Services.GetRequiredService<IBus>().Unsubscribe<UserRegisteredV1>();
            await subscriber.StopAsync();
            subscriber.Dispose();
            await QueueCleanup.DeleteAsync(fixture.RabbitMqConnectionString, queueName);
        }
    }

    private async Task<(IHost Host, string QueueName, EventCapture<UserRegisteredV1> Capture)> StartSubscriberAsync()
    {
        var capture = new EventCapture<UserRegisteredV1>();
        var queueName = $"identity-tests-sub.{Guid.NewGuid():N}";
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [RebusConfigurationExtensions.ConnectionStringConfigKey] = fixture.RabbitMqConnectionString,
        });
        builder.Services.AddSingleton(capture);
        builder.Services.AddBuildingBlocksRebus(builder.Configuration, queueName);
        builder.Services.AddRebusHandler<EventCapturingHandler>();
        var host = builder.Build();
        await host.StartAsync();
        await host.Services.GetRequiredService<IBus>().Subscribe<UserRegisteredV1>();
        return (host, queueName, capture);
    }
}
