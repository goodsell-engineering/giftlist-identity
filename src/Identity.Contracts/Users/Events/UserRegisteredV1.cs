namespace Identity.Contracts.Users.Events;

/// <summary>
/// Published after a new account is saved (ARCHITECTURE.md "Event catalogue": Identity → Gateway). The wire
/// counterpart of the domain event <c>Identity.Domain.Users.Events.UserRegistered</c> — mapped by
/// an Infrastructure event mapper, never published directly (ARCHITECTURE.md "Domain events are not integration events"). Carries no
/// password material.
/// </summary>
public sealed record UserRegisteredV1(Guid UserId, string Email, string DisplayName, DateTimeOffset RegisteredAt);
