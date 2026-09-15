using BuildingBlocks.Results;

namespace Identity.Application.Users;

/// <summary>
/// Stable, machine-readable error codes for the Users use cases (CONVENTIONS.md "Errors"), all
/// `identity.&lt;code&gt;` — two segments, applied uniformly, decided once rather than left to
/// drift per use case. Where SignUp and Login fail on the same semantic (an invalid email is an
/// invalid email regardless of which form asked for it), they share one code and one <see cref="Error"/>.
/// </summary>
public static class UserErrors
{
    public static readonly Error EmailAlreadyRegistered = new(
        "identity.email_already_registered",
        "A user with this email is already registered.",
        ErrorKind.Conflict);

    public static readonly Error EmailInvalid = new(
        "identity.email_invalid", "Enter a valid email address.", ErrorKind.Validation);

    public static readonly Error DisplayNameInvalid = new(
        "identity.display_name_invalid", "Enter a display name.", ErrorKind.Validation);

    public static Error PasswordTooShort(int minimumLength) => new(
        "identity.password_too_short",
        $"Password must be at least {minimumLength} characters.",
        ErrorKind.Validation);

    public static readonly Error PasswordRequired = new(
        "identity.password_required", "Enter your password.", ErrorKind.Validation);

    /// <summary>
    /// Deliberately the same error for "no such user" and "wrong password" — telling them apart
    /// would let a caller enumerate registered emails. <see cref="ErrorKind.Unauthenticated"/>:
    /// the caller is not authenticated, distinct from <see cref="ErrorKind.Forbidden"/> ("we know
    /// who you are and the answer is still no") — the SPA needs to tell "log in again" apart from
    /// "you may not do this".
    /// </summary>
    public static readonly Error InvalidCredentials = new(
        "identity.invalid_credentials",
        "Email or password is incorrect.",
        ErrorKind.Unauthenticated);
}
