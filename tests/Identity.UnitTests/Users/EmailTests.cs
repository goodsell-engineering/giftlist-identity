using Identity.Domain.Users;

namespace Identity.UnitTests.Users;

/// <summary>
/// The constructor's throw path is a gap the integration suite cannot reach: SignUpValidator
/// rejects a malformed email via <see cref="Email.IsValidFormat"/> before this constructor ever
/// runs, so end to end it always sees valid input (CONVENTIONS.md "Testing" — "error paths hard to
/// provoke end-to-end").
/// </summary>
public sealed class EmailTests
{
    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenValueIsNotAValidEmail()
    {
        // Arrange
        const string invalid = "not-an-email";

        // Act
        var exception = Record.Exception(() => new Email(invalid));

        // Assert
        var argumentException = Assert.IsType<ArgumentException>(exception);
        Assert.Equal("value", argumentException.ParamName);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenValueIsWhitespace()
    {
        // Arrange — none

        // Act
        var exception = Record.Exception(() => new Email("   "));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void Constructor_ShouldNormalizeToLowercaseAndTrimmed_SoTheUniqueIndexCatchesCaseVariants()
    {
        // Arrange
        const string mixedCase = "  Ada.Lovelace@Example.COM  ";

        // Act
        var email = new Email(mixedCase);

        // Assert
        Assert.Equal("ada.lovelace@example.com", email.Value);
    }

    [Fact]
    public void Equals_ShouldBeCaseInsensitive_BecauseBothSidesAreAlreadyNormalized()
    {
        // Arrange
        var first = new Email("Ada@Example.com");
        var second = new Email("ada@example.com");

        // Act
        var equal = first.Equals(second);

        // Assert
        Assert.True(equal);
    }

    [Fact]
    public void IsValidFormat_ShouldReturnFalse_WhenValueIsNullOrWhitespace()
    {
        // Arrange — none

        // Act
        var validNull = Email.IsValidFormat(null);
        var validEmpty = Email.IsValidFormat("   ");

        // Assert
        Assert.False(validNull);
        Assert.False(validEmpty);
    }

    [Fact]
    public void ToString_ShouldReturnTheNormalizedValue()
    {
        // Arrange
        var email = new Email("Ada@Example.com");

        // Act
        var text = email.ToString();

        // Assert
        Assert.Equal("ada@example.com", text);
    }
}
