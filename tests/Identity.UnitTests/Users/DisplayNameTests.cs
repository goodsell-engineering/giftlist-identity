using Identity.Domain.Users;

namespace Identity.UnitTests.Users;

public sealed class DisplayNameTests
{
    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenValueExceedsTheMaximumLength()
    {
        // Arrange
        var tooLong = new string('a', 101);

        // Act
        var exception = Record.Exception(() => new DisplayName(tooLong));

        // Assert
        var argumentException = Assert.IsType<ArgumentException>(exception);
        Assert.Equal("value", argumentException.ParamName);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenValueIsNullOrWhitespace()
    {
        // Arrange — none

        // Act
        var exception = Record.Exception(() => new DisplayName(""));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void Constructor_ShouldTrimSurroundingWhitespace()
    {
        // Arrange
        const string padded = "  Ada Lovelace  ";

        // Act
        var displayName = new DisplayName(padded);

        // Assert
        Assert.Equal("Ada Lovelace", displayName.Value);
    }

    [Fact]
    public void IsValidLength_ShouldReturnFalse_WhenValueExceedsTheMaximumLength()
    {
        // Arrange
        var tooLong = new string('a', 101);

        // Act
        var valid = DisplayName.IsValidLength(tooLong);

        // Assert
        Assert.False(valid);
    }

    [Fact]
    public void IsValidLength_ShouldReturnTrue_AtExactlyTheMaximumLength()
    {
        // Arrange
        var exactlyMax = new string('a', 100);

        // Act
        var valid = DisplayName.IsValidLength(exactlyMax);

        // Assert
        Assert.True(valid);
    }
}
