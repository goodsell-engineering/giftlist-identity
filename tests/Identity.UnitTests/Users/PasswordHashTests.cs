using Identity.Domain.Users;

namespace Identity.UnitTests.Users;

public sealed class PasswordHashTests
{
    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenValueIsShorterThanTheMinimumLength()
    {
        // Arrange
        const string tooShort = "short-hash";

        // Act
        var exception = Record.Exception(() => new PasswordHash(tooShort));

        // Assert
        var argumentException = Assert.IsType<ArgumentException>(exception);
        Assert.Equal("value", argumentException.ParamName);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenValueIsNullOrWhitespace()
    {
        // Arrange — none

        // Act
        var exception = Record.Exception(() => new PasswordHash("   "));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void ToString_ShouldNeverExposeTheHash_EvenInALogOrExceptionMessage()
    {
        // Arrange
        var hash = new PasswordHash("$2a$12$abcdefghijklmnopqrstuvwxyzABCDEF");

        // Act
        var text = hash.ToString();

        // Assert
        Assert.Equal("[redacted]", text);
        Assert.DoesNotContain("2a$12", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Equals_ShouldCompareByValue_ForTwoEqualHashes()
    {
        // Arrange
        const string value = "$2a$12$abcdefghijklmnopqrstuvwxyzABCDEF";
        var first = new PasswordHash(value);
        var second = new PasswordHash(value);

        // Act
        var equal = first.Equals(second);

        // Assert
        Assert.True(equal);
    }
}
