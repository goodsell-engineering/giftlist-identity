using System.Text.RegularExpressions;

namespace Identity.Domain.Users;

/// <summary>
/// A validated, lower-cased email address (CONVENTIONS.md "Domain modelling" — value objects for anything with a
/// rule). Normalising case here, once, is what makes the Mongo unique index on email actually
/// enforce "one account per address" instead of letting "A@x.com" and "a@x.com" both register.
/// </summary>
public sealed class Email : IEquatable<Email>
{
    /// <summary>
    /// GL-44 security pass: the regex below has no length of its own — `[^@\s]+` matches any
    /// number of non-whitespace, non-`@` characters, so an otherwise-shaped value could be
    /// arbitrarily long before this existed, an unbounded write into the same Mongo unique index
    /// <see cref="Value"/>'s own doc comment describes (the same "bare, unbounded string written
    /// to storage with no rule anywhere" CONVENTIONS.md "Domain modelling" calls out — the same
    /// class of gap GL-74 closed for <c>GiftItemUrl</c>/<c>GiftItemDescription</c>). 254 is
    /// RFC 5321's own practical limit on the length of a complete email address (the "Path"
    /// production, section 4.5.3.1.3) — not a number picked for this demo, the actual wire limit
    /// a compliant SMTP server is required to accept.
    /// </summary>
    public const int MaxLength = 254;

    private static readonly Regex Pattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public Email(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > MaxLength)
        {
            throw new ArgumentException($"Value must be at most {MaxLength} characters.", nameof(value));
        }

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
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= MaxLength && Pattern.IsMatch(value.Trim());

    public bool Equals(Email? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as Email);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
