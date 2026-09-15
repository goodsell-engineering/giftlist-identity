namespace Identity.Domain.Users;

/// <summary>
/// Strongly-typed id (CONVENTIONS.md "Domain modelling") — a bare <see cref="Guid"/> would let a caller pass a
/// gift-list id or any other GUID-shaped value where a user id belongs and the compiler would
/// never notice.
/// </summary>
public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
