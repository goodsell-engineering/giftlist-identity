using Identity.Domain.Common;
using Identity.Domain.Users.Events;

namespace Identity.Domain.Users;

/// <summary>The Identity service's one aggregate root (ARCHITECTURE.md "Data model" — <c>identity.users</c>).</summary>
public sealed class User
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private User(UserId id, Email email, PasswordHash passwordHash, DisplayName displayName, DateTimeOffset createdAt)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        // Truncated to what Mongo can round-trip — see Timestamps.ToStoredPrecision (GL-63,
        // mirroring GiftLists.Domain.GiftLists.GiftList's own constructor).
        CreatedAt = Timestamps.ToStoredPrecision(createdAt);
    }

    public UserId Id { get; }

    public Email Email { get; }

    public PasswordHash PasswordHash { get; }

    public DisplayName DisplayName { get; }

    public DateTimeOffset CreatedAt { get; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    /// <summary>
    /// Registers a brand-new account, raising <see cref="UserRegistered"/>. The only path that
    /// raises a domain event — <see cref="Rehydrate"/> never does, since loading an existing user
    /// back out of storage is not something newly happening (CONVENTIONS.md "Domain modelling").
    /// </summary>
    /// <param name="registeredAt">
    /// Sourced from the caller's clock port rather than <see cref="DateTimeOffset.UtcNow"/> — the
    /// aggregate has no project references at all (CONVENTIONS.md "Project reference graph") and must stay deterministic
    /// for tests.
    /// </param>
    public static User Register(
        UserId id,
        Email email,
        PasswordHash passwordHash,
        DisplayName displayName,
        DateTimeOffset registeredAt)
    {
        var user = new User(id, email, passwordHash, displayName, registeredAt);
        // user.CreatedAt, NOT the raw registeredAt parameter: the constructor normalised it, and
        // an event carrying the un-normalised value would tell the Gateway something that
        // disagrees with what identity.users holds (GL-63, mirroring the same fix in
        // GiftLists.Domain.GiftLists.GiftList.Create).
        user._domainEvents.Add(new UserRegistered(id, email, displayName, user.CreatedAt));
        return user;
    }

    /// <summary>
    /// Rebuilds a <see cref="User"/> from persisted state. Called only by the Infrastructure
    /// mapper (CONVENTIONS.md "Domain modelling" — no public parameterless constructor; rehydration goes through
    /// the mapper). Never raises domain events.
    /// </summary>
    public static User Rehydrate(
        UserId id,
        Email email,
        PasswordHash passwordHash,
        DisplayName displayName,
        DateTimeOffset createdAt) =>
        new(id, email, passwordHash, displayName, createdAt);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
