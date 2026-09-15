using Identity.Contracts.Users.Events;

namespace Identity.UnitTests.Users;

/// <summary>
/// A static shape guard for the wire contract (ARCHITECTURE.md "Domain events are not integration events" — integration events, never
/// domain events, leave the process). Moved here from Identity.IntegrationTests (GL-57 review):
/// this needs no broker and no SignUp call — reflecting over the type itself is enough to catch a
/// field carrying password material added later, and belongs beside the other pure checks rather
/// than dressed up as an end-to-end assertion.
/// </summary>
public sealed class UserRegisteredV1Tests
{
    [Fact]
    public void UserRegisteredV1_ShouldExposeExactlyItsFourKnownFields_AndNeverPasswordMaterial()
    {
        // Arrange — none

        // Act
        var propertyNames = typeof(UserRegisteredV1).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

        // Assert
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal) { "UserId", "Email", "DisplayName", "RegisteredAt" },
            propertyNames);
    }
}
