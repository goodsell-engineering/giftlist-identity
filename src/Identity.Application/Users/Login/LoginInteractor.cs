using BuildingBlocks.Results;
using Identity.Application.Common;
using Identity.Domain.Users;

namespace Identity.Application.Users.Login;

/// <summary>
/// Assumes its request already passed <see cref="LoginValidator"/> — validation is the
/// composition root's decorator's job (CONVENTIONS.md "Use cases"), not this interactor's.
/// </summary>
internal sealed class LoginInteractor : ILogin
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenIssuer _tokenIssuer;

    public LoginInteractor(IUserRepository users, IPasswordHasher passwordHasher, ITokenIssuer tokenIssuer)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
    }

    public async Task<Result<LoginResponse>> Handle(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = new Email(request.Email);
        var user = await _users.FindByEmailAsync(email, cancellationToken);

        // Same error for "no such user" and "wrong password" (UserErrors.InvalidCredentials'
        // own doc comment) — telling them apart would let a caller enumerate registered emails.
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash.Value))
        {
            return UserErrors.InvalidCredentials;
        }

        var accessToken = _tokenIssuer.Issue(user.Id, user.Email, user.DisplayName);
        return new LoginResponse(user.Id.Value, accessToken.Value, accessToken.ExpiresAt);
    }
}
