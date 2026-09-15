using Identity.Contracts.Users.Events;
using Identity.Domain.Common;
using Identity.Domain.Users.Events;

namespace Identity.Infrastructure.Users.Messaging;

/// <summary>
/// Translates domain events into the integration events Identity actually publishes
/// (ARCHITECTURE.md "Domain events are not integration events" — and they never leave the process
/// directly). The one-to-one shape looks like ceremony with a single domain event today; it is
/// the seam that lets the domain change without a wire-breaking change tomorrow.
/// </summary>
internal static class UserEventMapper
{
    public static object ToIntegrationEvent(IDomainEvent domainEvent) => domainEvent switch
    {
        UserRegistered e => new UserRegisteredV1(e.UserId.Value, e.Email.Value, e.DisplayName.Value, e.RegisteredAt),
        _ => throw new InvalidOperationException(
            $"No integration event mapping for domain event '{domainEvent.GetType().Name}'."),
    };

    /// <summary>
    /// The id of the user an event concerns, for diagnostics. Lives here, beside the exhaustive
    /// mapping switch, so per-event-type knowledge stays in one place rather than being
    /// re-derived by a caller that only needs one field (mirrors GiftLists' <c>ListIdOf</c>,
    /// GL-62). Non-throwing by design, for the same reason as <c>ListIdOf</c>: it is called from
    /// <see cref="UserEventPublisher"/>'s catch block, and it must never be the thing that throws
    /// there.
    /// </summary>
    public static Guid UserIdOf(IDomainEvent domainEvent) => domainEvent switch
    {
        UserRegistered e => e.UserId.Value,
        _ => Guid.Empty,
    };
}
