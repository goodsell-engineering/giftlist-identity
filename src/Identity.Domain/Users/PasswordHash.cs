namespace Identity.Domain.Users;

/// <summary>
/// An already-hashed password. Never constructed from plaintext — an
/// <c>Identity.Application.Users.IPasswordHasher</c> (an Infrastructure adapter, per
/// CONVENTIONS.md "Project reference graph") produces the hash; this type only ever wraps the result.
///
/// Equality is ordinary value equality, but it is deliberately never used to authenticate a
/// login: verifying a password is "does the hasher's own (timing-safe, algorithm-aware) compare
/// say yes", not "do two hash strings match" (CONVENTIONS.md "Domain modelling"). <see cref="ToString"/> redacts
/// the value so a stray log statement or exception message never leaks it.
/// </summary>
public sealed class PasswordHash : IEquatable<PasswordHash>
{
    private const int MinimumLength = 20;

    public PasswordHash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Length < MinimumLength)
        {
            throw new ArgumentException("Value does not look like a hashed password.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public bool Equals(PasswordHash? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as PasswordHash);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => "[redacted]";
}
