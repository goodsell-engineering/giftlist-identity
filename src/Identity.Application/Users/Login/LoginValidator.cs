using BuildingBlocks.Results;
using Identity.Application.Common;
using Identity.Domain.Users;

namespace Identity.Application.Users.Login;

internal sealed class LoginValidator : IValidator<LoginRequest>
{
    public Result Validate(LoginRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Email.IsValidFormat(request.Email))
        {
            return UserErrors.EmailInvalid;
        }

        if (string.IsNullOrEmpty(request.Password))
        {
            return UserErrors.PasswordRequired;
        }

        return Result.Success();
    }
}
