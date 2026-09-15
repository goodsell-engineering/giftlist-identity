using System.Text.RegularExpressions;

namespace Identity.Domain.Users;

/// <summary>
/// A validated, lower-cased email address (CONVENTIONS.md "Domain modelling" — value objects for anything with a
/// rule). Normalising case here, once, is what makes the Mongo unique index on email actually
/// enforce "one account per address" instead of letting "A@x.com" and "a@x.com" both register.
/// </summary>
public sealed class Email : IEquatable<Email>
{
    private static readonly Regex Pattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public Email(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Trim().ToLowerInvariant();
        if (!Pattern.IsMatch(normalized))
        {
            throw new ArgumentException("Value is not a valid email address.", nameof(value));
        }

        Value = normalized;
    }

    public string Value { get; }

    /// <summary>
    /// The same rule the constructor enforces, exposed as a non-throwing predicate so an
    /// Application-ring validator (CONVENTIONS.md "Use cases") can reject bad input as a
    /// <c>Result</c> before it ever reaches the constructor. By the time this constructor runs,
    /// invalid input indicates a missed validation step, not a user mistake (CONVENTIONS.md "Errors").
    /// </summary>
    public static bool IsValidFormat(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Pattern.IsMatch(value.Trim());

    public bool Equals(Email? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as Email);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
