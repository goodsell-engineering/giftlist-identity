using BuildingBlocks.Results;
using Identity.Application.Common;
using Identity.Domain.Users;

namespace Identity.Application.Users.SignUp;

/// <summary>
/// Assumes its request already passed <see cref="SignUpValidator"/> — validation is the
/// composition root's decorator's job (CONVENTIONS.md "Use cases"), not this interactor's.
/// </summary>
internal sealed class SignUpInteractor : ISignUp
{
    private readonly IUserRepository _users;
    private readonly IDomainEventPublisher _userEvents;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IClock _clock;

    public SignUpInteractor(
        IUserRepository users,
        IDomainEventPublisher userEvents,
        IPasswordHasher passwordHasher,
        ITokenIssuer tokenIssuer,
        IClock clock)
    {
        _users = users;
        _userEvents = userEvents;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
        _clock = clock;
    }

    public async Task<Result<SignUpResponse>> Handle(SignUpRequest request, CancellationToken cancellationToken)
    {
        var email = new Email(request.Email);

        var existing = await _users.FindByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            return UserErrors.EmailAlreadyRegistered;
        }

        var displayName = new DisplayName(request.DisplayName);
        var passwordHash = new PasswordHash(_passwordHasher.Hash(request.Password));
        var user = User.Register(UserId.New(), email, passwordHash, displayName, _clock.UtcNow);

        // The upfront check above is a UX nicety; this is the actual source of truth — the
        // unique index rejects the insert if a concurrent sign-up won the same email
        // (ARCHITECTURE.md "Data model"). Either way it is an expected outcome, returned as a value, never
        // an exception (CONVENTIONS.md "Errors").
        var saved = await _users.AddAsync(user, cancellationToken);
        if (saved.IsFailure)
        {
            return Result<SignUpResponse>.Failure(saved.Error);
        }

        // Save first, publish second (ARCHITECTURE.md "Event publishing: synchronous") — a published event describing a
        // write that didn't happen is worse than a write nobody heard about. The two are not
        // atomic: if the publish below fails, the account exists but nothing downstream (the
        // Gateway's read model) ever learns of it via UserRegisteredV1. ARCHITECTURE.md "Event publishing: synchronous"
        // accepts that dual-write risk knowingly for this demo — no outbox — so this is a
        // decision, not an oversight; recovery is manual restart/replay.
        await _userEvents.PublishAsync(user.DomainEvents, cancellationToken);
        user.ClearDomainEvents();

        var accessToken = _tokenIssuer.Issue(user.Id, user.Email, user.DisplayName);
        return new SignUpResponse(user.Id.Value, accessToken.Value, accessToken.ExpiresAt);
    }
}
