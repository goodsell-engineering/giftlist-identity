using BuildingBlocks.Results;
using Identity.Application.Users;
using Identity.Contracts.Users;
using Identity.Domain.Users;
using Identity.Infrastructure.Users.Persistence;
using Identity.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Identity.IntegrationTests.Users;

/// <summary>
/// The unique index on <c>users.email</c> is the only thing standing between two concurrent
/// sign-ups and two accounts for one address — <c>SignUpInteractor</c>'s upfront
/// <c>FindByEmailAsync</c> check is a UX nicety, not the source of truth (its own doc comment).
/// Only a real Mongo index can be raced like this; an in-memory repository would let every
/// concurrent insert through.
/// </summary>
[Collection(IdentityCollection.Name)]
public sealed class ConcurrentSignUpTests(IdentityFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SignUp_ShouldLetExactlyOneWin_WhenTwoRequestsRaceForTheSameEmail()
    {
        // Arrange — enough concurrent contenders that at least one pair genuinely overlaps past
        // the interactor's own existence check and reaches InsertOneAsync together, which is
        // what actually exercises the index rather than the earlier, best-effort check. An
        // explicit, generous timeout (GL-57 review): eight BCrypt-factor-12 hashes racing on a
        // contended box could plausibly cross the bridge's ~5s default, and a timeout here would
        // surface as messaging.reply_timeout failing the email_already_registered assertion
        // below for an unrelated reason.
        var email = $"race-{Guid.NewGuid():N}@example.com";
        const int contenders = 8;
        var generousTimeout = TimeSpan.FromSeconds(30);

        // Act
        var results = await Task.WhenAll(Enumerable.Range(0, contenders).Select(i =>
            fixture.RequestReplyBridge.SendAndAwaitReply<SignUpReply>(
                new SignUp(email, "correct horse battery staple", $"Contender {i}"), generousTimeout)));

        // Assert
        var successes = results.Count(r => r.IsSuccess);
        var conflicts = results.Where(r => r.IsFailure).ToList();
        Assert.Equal(1, successes);
        Assert.Equal(contenders - 1, conflicts.Count);
        Assert.All(conflicts, r => Assert.Equal("identity.email_already_registered", r.Error.Code));
        Assert.All(conflicts, r => Assert.Equal(ErrorKind.Conflict, r.Error.Kind));

        var collection = fixture.Database.GetCollection<UserDocument>(IdentityFixture.UsersCollectionName);
        var storedCount = await collection.CountDocumentsAsync(u => u.Email == email.ToLowerInvariant());
        Assert.Equal(1, storedCount);
    }

    [Fact]
    public async Task AddAsync_ShouldReturnEmailAlreadyRegistered_ForASecondUserWithTheSameEmail_BypassingThePreCheck()
    {
        // Arrange — GL-57 review: the race test above is genuinely racy but cannot distinguish
        // a conflict raised by SignUpInteractor's pre-check from one raised by the index itself
        // — both return the same code, so dropping the index would still leave that test green.
        // Calling IUserRepository.AddAsync directly, twice, for the same email skips the
        // pre-check entirely and goes red the instant the unique index is gone, deterministically
        // rather than under a timing window.
        using var scope = fixture.CreateIdentityScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var email = new Email($"direct-{Guid.NewGuid():N}@example.com");
        var passwordHash = new PasswordHash("$2a$12$abcdefghijklmnopqrstuvwxyzABCDEF");
        var first = User.Register(UserId.New(), email, passwordHash, new DisplayName("First Contender"), DateTimeOffset.UtcNow);
        var second = User.Register(UserId.New(), email, passwordHash, new DisplayName("Second Contender"), DateTimeOffset.UtcNow);

        // Act
        var firstResult = await repository.AddAsync(first, CancellationToken.None);
        var secondResult = await repository.AddAsync(second, CancellationToken.None);

        // Assert
        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsFailure);
        Assert.Equal("identity.email_already_registered", secondResult.Error.Code);
        Assert.Equal(ErrorKind.Conflict, secondResult.Error.Kind);
    }

    [Fact]
    public async Task UsersCollection_ShouldHaveAUniqueIndexOnEmail()
    {
        // Arrange — asserted directly (CONVENTIONS.md "Persistence": "deserves a test that fails if someone
        // drops it"), independent of whether the race test above happens to trigger it this run.
        var collection = fixture.Database.GetCollection<UserDocument>(IdentityFixture.UsersCollectionName);

        // Act
        var indexes = await (await collection.Indexes.ListAsync()).ToListAsync();

        // Assert
        var emailIndex = indexes.SingleOrDefault(i => i["name"] == "email_unique");
        Assert.NotNull(emailIndex);
        Assert.True(emailIndex["unique"].AsBoolean);
    }
}
