using Identity.Domain.Common;

namespace Identity.Application.Common;

/// <summary>
/// Publishes an aggregate's saved domain events as integration events (ARCHITECTURE.md "Domain events are not integration events").
/// Kept separate from a repository's <c>AddAsync</c> deliberately — a repository that both
/// writes and publishes conflates persistence with messaging. Genuinely domain-agnostic in
/// signature (it never mentions <c>User</c> specifically), so it lives in <c>Common/</c> next to
/// <see cref="IClock"/>/<see cref="ITokenIssuer"/> rather than beside
/// <c>Identity.Application.Users.IUserRepository</c> — even though, with Identity's one
/// aggregate, its only implementation today is Users-specific. The interactor calls this only
/// after the repository's save has succeeded — the order ARCHITECTURE.md "Event publishing: synchronous" requires — and the
/// mapping from domain to integration event happens behind this port, in Infrastructure, never
/// in Application (ARCHITECTURE.md "Domain events are not integration events").
/// </summary>
public interface IDomainEventPublisher
{
    Task PublishAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken);
}
