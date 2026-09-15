using BuildingBlocks.Results;
using Identity.Application.Common;
using Identity.Domain.Users;

namespace Identity.Application.Users.SignUp;

/// <summary>
/// Hand-rolled (CONVENTIONS.md "Project reference graph" — Application references no validation library), and checked
/// against the same rules <see cref="Email"/>/<see cref="DisplayName"/> enforce via their static
/// predicates rather than duplicating the regex/length checks in two places.
/// </summary>
internal sealed class SignUpValidator : IValidator<SignUpRequest>
{
    private const int MinimumPasswordLength = 8;

    public Result Validate(SignUpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Email.IsValidFormat(request.Email))
        {
            return UserErrors.EmailInvalid;
        }

        if (!DisplayName.IsValidLength(request.DisplayName))
        {
            return UserErrors.DisplayNameInvalid;
        }

        if (string.IsNullOrEmpty(request.Password) || request.Password.Length < MinimumPasswordLength)
        {
            return UserErrors.PasswordTooShort(MinimumPasswordLength);
        }

        return Result.Success();
    }
}
