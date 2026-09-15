namespace Identity.Domain.Users;

/// <summary>The name a user is shown as. A value object purely for its length rule (CONVENTIONS.md "Domain modelling").</summary>
public sealed class DisplayName : IEquatable<DisplayName>
{
    private const int MaxLength = 100;

    public DisplayName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            throw new ArgumentException($"Value must be at most {MaxLength} characters.", nameof(value));
        }

        Value = trimmed;
    }

    public string Value { get; }

    /// <summary>
    /// The same rule the constructor enforces, exposed as a non-throwing predicate — see
    /// <see cref="Email.IsValidFormat"/> for why Application-ring validation needs this shape.
    /// </summary>
    public static bool IsValidLength(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= MaxLength;

    public bool Equals(DisplayName? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as DisplayName);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
