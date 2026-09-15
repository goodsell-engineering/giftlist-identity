using Identity.Domain.Common;

namespace Identity.Domain.Users.Events;

/// <summary>
/// Raised when a new account is created. Never leaves the process — an Infrastructure mapper
/// translates this into the wire-facing UserRegisteredV1 integration event for publication
/// (ARCHITECTURE.md "Domain events are not integration events"). Carries no password material by construction.
/// </summary>
public sealed record UserRegistered(UserId UserId, Email Email, DisplayName DisplayName, DateTimeOffset RegisteredAt)
    : IDomainEvent;
