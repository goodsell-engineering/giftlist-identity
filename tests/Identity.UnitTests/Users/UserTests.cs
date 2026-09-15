using Identity.Domain.Users;
using Identity.Domain.Users.Events;

namespace Identity.UnitTests.Users;

/// <summary>
/// <see cref="User.Register"/> vs. <see cref="User.Rehydrate"/> is a distinction the integration
/// suite exercises only indirectly (a successful SignUp implies an event was raised, since
/// UserEventPublishingTests observes it on the wire) — the "Rehydrate never raises one" half is
/// unreachable end to end, since nothing there ever loads an existing user back out of storage
/// through a path that could observe domain events.
/// </summary>
public sealed class UserTests
{
    private static readonly UserId Id = UserId.New();
    private static readonly Email Email = new("ada@example.com");
    private static readonly PasswordHash PasswordHash = new("$2a$12$abcdefghijklmnopqrstuvwxyzABCDEF");
    private static readonly DisplayName DisplayName = new("Ada Lovelace");
    private static readonly DateTimeOffset RegisteredAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_ShouldRaiseAUserRegisteredEvent_WithTheGivenFields()
    {
        // Arrange — none

        // Act
        var user = User.Register(Id, Email, PasswordHash, DisplayName, RegisteredAt);

        // Assert
        var domainEvent = Assert.Single(user.DomainEvents);
        var userRegistered = Assert.IsType<UserRegistered>(domainEvent);
        Assert.Equal(Id, userRegistered.UserId);
        Assert.Equal(Email, userRegistered.Email);
        Assert.Equal(DisplayName, userRegistered.DisplayName);
        Assert.Equal(RegisteredAt, userRegistered.RegisteredAt);
    }

    /// <summary>
    /// The event must carry the instant the aggregate actually KEPT, not the raw parameter.
    /// <see cref="Register_ShouldRaiseAUserRegisteredEvent_WithTheGivenFields"/> cannot see this:
    /// its <c>RegisteredAt</c> is a whole second, so normalising it is a no-op and that assertion
    /// passes either way. Verified by mutation (GL-63 review) — reverting the event to the raw
    /// parameter left all 55 unit tests green, and no integration test asserts this field either.
    ///
    /// It matters because <c>UserEventMapper</c> copies this straight onto
    /// <c>UserRegisteredV1</c>: an un-normalised value here is published to the Gateway and
    /// disagrees, by up to 9999 ticks, with what <c>identity.users</c> holds for the same user.
    /// This is the defect GL-66 fixed for GiftLists' five events, in Identity's one.
    /// </summary>
    [Fact]
    public void Register_ShouldRaiseTheEvent_CarryingTheNormalisedInstant_NotTheRawArgument()
    {
        // Arrange — sub-millisecond ticks are what storage discards, so the raw argument and the
        // stored value differ here; at whole-second precision they would not.
        var withSubMillisecondTicks = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddTicks(9_999);

        // Act
        var user = User.Register(Id, Email, PasswordHash, DisplayName, withSubMillisecondTicks);

        // Assert
        var userRegistered = Assert.IsType<UserRegistered>(Assert.Single(user.DomainEvents));
        Assert.Equal(user.CreatedAt, userRegistered.RegisteredAt);
        Assert.NotEqual(withSubMillisecondTicks, userRegistered.RegisteredAt);
        Assert.Equal(0, userRegistered.RegisteredAt.Ticks % TimeSpan.TicksPerMillisecond);
    }

    [Fact]
    public void Rehydrate_ShouldRaiseNoDomainEvents()
    {
        // Arrange — none

        // Act
        var user = User.Rehydrate(Id, Email, PasswordHash, DisplayName, RegisteredAt);

        // Assert
        Assert.Empty(user.DomainEvents);
    }

    [Fact]
    public void ClearDomainEvents_ShouldEmptyTheCollection_AfterRegister()
    {
        // Arrange
        var user = User.Register(Id, Email, PasswordHash, DisplayName, RegisteredAt);

        // Act
        user.ClearDomainEvents();

        // Assert
        Assert.Empty(user.DomainEvents);
    }

    [Fact]
    public void Register_ShouldSetAllProperties_FromTheGivenValues()
    {
        // Arrange — none

        // Act
        var user = User.Register(Id, Email, PasswordHash, DisplayName, RegisteredAt);

        // Assert
        Assert.Equal(Id, user.Id);
        Assert.Equal(Email, user.Email);
        Assert.Equal(PasswordHash, user.PasswordHash);
        Assert.Equal(DisplayName, user.DisplayName);
        Assert.Equal(RegisteredAt, user.CreatedAt);
    }
}
