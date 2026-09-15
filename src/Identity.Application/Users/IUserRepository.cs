using BuildingBlocks.Results;
using Identity.Domain.Users;

namespace Identity.Application.Users;

/// <summary>Port lives beside the domain it serves (CONVENTIONS.md "Folder structure"), not in a shared Abstractions bucket.</summary>
public interface IUserRepository
{
    Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken);

    /// <summary>
    /// Persists a brand-new user. Publishing its domain events is a separate step the interactor
    /// drives through <see cref="IUserEventPublisher"/> after this succeeds (ARCHITECTURE.md
    /// "Domain events are not integration events" — save, then a mapper translates, then a publisher sends; this port is only the
    /// first of those). Returns <see cref="UserErrors.EmailAlreadyRegistered"/> if the unique
    /// index on email rejects the insert — the race-safety net behind the interactor's upfront
    /// existence check — rather than throwing: an expected outcome is a value (CONVENTIONS.md "Errors").
    /// </summary>
    Task<Result> AddAsync(User user, CancellationToken cancellationToken);
}
