using Identity.Application.Users;
using Identity.Domain.Users;
using Identity.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.IntegrationTests.Users;

/// <summary>
/// Exercises <see cref="IUserRepository"/> directly against real Mongo — the round-trip
/// assertion GiftLists' <c>GiftListRepositoryTests</c> has and Identity previously lacked (GL-63,
/// raised by the Batch 11 review of GL-20: nothing here asserted a <c>User</c> round-trips equal,
/// so the same BSON-millisecond-vs-tick defect GL-20 fixed for gift lists was latent for users).
/// </summary>
[Collection(IdentityCollection.Name)]
public sealed class UserRepositoryTests(IdentityFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task FindByEmailAsync_ShouldRoundTripEveryField_ThroughRealMongo()
    {
        // Arrange
        var id = UserId.New();
        var email = new Email($"round-trip-{Guid.NewGuid():N}@example.com");
        var passwordHash = new PasswordHash("$2a$12$abcdefghijklmnopqrstuvwxyzABCDEF");
        var displayName = new DisplayName("Ada Lovelace");
        var registeredAt = DateTimeOffset.UtcNow;
        var user = User.Register(id, email, passwordHash, displayName, registeredAt);

        using var scope = fixture.CreateIdentityScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var saved = await repository.AddAsync(user, CancellationToken.None);
        Assert.True(saved.IsSuccess);

        // Act
        var reloaded = await repository.FindByEmailAsync(email, CancellationToken.None);

        // Assert
        Assert.NotNull(reloaded);
        Assert.Equal(user.Id, reloaded.Id);
        Assert.Equal(user.Email, reloaded.Email);
        Assert.Equal(user.PasswordHash, reloaded.PasswordHash);
        Assert.Equal(user.DisplayName, reloaded.DisplayName);
        // Compared against the aggregate's own stored value, not the raw `registeredAt` local:
        // the claim under test is that a reload equals what was SAVED. User.CreatedAt normalises
        // to millisecond precision at construction (Timestamps.ToStoredPrecision), so comparing
        // to the un-normalised input would be asserting a precision the system deliberately does
        // not keep (mirrors GiftListRepositoryTests' own round-trip test, GL-20/GL-63).
        Assert.Equal(user.CreatedAt, reloaded.CreatedAt);
    }
}
