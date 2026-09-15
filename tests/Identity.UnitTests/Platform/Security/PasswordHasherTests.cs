using Identity.UnitTests.Support;

namespace Identity.UnitTests.Platform.Security;

/// <summary>BCrypt round-tripping through <c>IPasswordHasher</c> — see IntegrationTests for the end-to-end SignUp/Login path this feeds.</summary>
public sealed class PasswordHasherTests
{
    [Fact]
    public void Verify_ShouldReturnTrue_WhenThePasswordMatchesItsOwnHash()
    {
        // Arrange
        var hasher = InfrastructurePorts.BuildPasswordHasher();
        var hash = hasher.Hash("correct horse battery staple");

        // Act
        var verified = hasher.Verify("correct horse battery staple", hash);

        // Assert
        Assert.True(verified);
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WhenThePasswordDoesNotMatch()
    {
        // Arrange
        var hasher = InfrastructurePorts.BuildPasswordHasher();
        var hash = hasher.Hash("correct horse battery staple");

        // Act
        var verified = hasher.Verify("totally the wrong password", hash);

        // Assert
        Assert.False(verified);
    }

    [Fact]
    public void Hash_ShouldProduceADifferentHash_ForTheSamePasswordEachTime()
    {
        // Arrange — BCrypt salts per call; two hashes of the same password must never collide,
        // or a leaked hash table would let two identical passwords be spotted by inspection.
        var hasher = InfrastructurePorts.BuildPasswordHasher();

        // Act
        var first = hasher.Hash("correct horse battery staple");
        var second = hasher.Hash("correct horse battery staple");

        // Assert
        Assert.NotEqual(first, second);
        Assert.True(hasher.Verify("correct horse battery staple", first));
        Assert.True(hasher.Verify("correct horse battery staple", second));
    }

    [Fact]
    public void Hash_ShouldProduceAValueLongEnoughForPasswordHashsOwnMinimumLength()
    {
        // Arrange — Identity.Domain.Users.PasswordHash's constructor rejects anything under 20
        // characters as "doesn't look like a hashed password"; the real hasher must clear that
        // bar, or every successful sign-up would throw constructing the aggregate.
        var hasher = InfrastructurePorts.BuildPasswordHasher();

        // Act
        var hash = hasher.Hash("correct horse battery staple");

        // Assert
        Assert.True(hash.Length >= 20);
    }
}
