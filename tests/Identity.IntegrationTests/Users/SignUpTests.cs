using BuildingBlocks.Results;
using Identity.Contracts.Users;
using Identity.Infrastructure.Users.Persistence;
using Identity.IntegrationTests.Fixtures;
using Identity.IntegrationTests.Support;
using MongoDB.Driver;

namespace Identity.IntegrationTests.Users;

/// <summary>
/// Enters at Identity's real entry point — the <c>SignUpHandler</c> Rebus handler, reached by
/// sending the wire <see cref="SignUp"/> command exactly the way the Gateway would (GL-2's "sign
/// up... through the browser" exit criterion). All infrastructure is real (CONVENTIONS.md "Testing").
/// </summary>
[Collection(IdentityCollection.Name)]
public sealed class SignUpTests(IdentityFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SignUp_ShouldReturnAnAccessToken_WhenTheEmailIsNew()
    {
        // Arrange
        var command = new SignUp($"new-{Guid.NewGuid():N}@example.com", "correct horse battery staple", "Ada Lovelace");

        // Act
        var result = await fixture.RequestReplyBridge.SendAndAwaitReply<SignUpReply>(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.AccessToken);
        Assert.NotEqual(Guid.Empty, result.Value.UserId);
    }

    [Fact]
    public async Task SignUp_ShouldIssueATokenWithTheExpectedClaimsIssuerAudienceAndExpiry()
    {
        // Arrange
        var command = new SignUp($"claims-{Guid.NewGuid():N}@example.com", "correct horse battery staple", "Ada Lovelace");
        var before = DateTimeOffset.UtcNow;

        // Act
        var result = await fixture.RequestReplyBridge.SendAndAwaitReply<SignUpReply>(command);

        // Assert
        Assert.True(result.IsSuccess);
        var (principal, token) = JwtAssertions.Validate(result.Value.AccessToken);
        Assert.Equal(result.Value.UserId.ToString(), principal.FindFirst("sub")?.Value);
        Assert.Equal(command.Email, principal.FindFirst("email")?.Value);
        Assert.Equal("Ada Lovelace", principal.FindFirst("name")?.Value);
        Assert.Equal(TestJwtKeys.Issuer, token.Issuer);
        Assert.Contains(TestJwtKeys.Audience, token.Audiences);
        Assert.True(token.ValidTo > before, "Token should expire in the future.");
        Assert.True(token.ValidTo <= before.AddMinutes(15).UtcDateTime.AddSeconds(30), "Token lifetime should match Jwt:AccessTokenLifetime.");
    }

    [Fact]
    public async Task SignUp_ShouldPersistTheUserWithANormalizedEmail_WhenSuccessful()
    {
        // Arrange
        var mixedCaseEmail = $"Mixed-{Guid.NewGuid():N}@Example.COM";
        var command = new SignUp(mixedCaseEmail, "correct horse battery staple", "Ada Lovelace");

        // Act
        var result = await fixture.RequestReplyBridge.SendAndAwaitReply<SignUpReply>(command);

        // Assert
        Assert.True(result.IsSuccess);
        var collection = fixture.Database.GetCollection<UserDocument>(IdentityFixture.UsersCollectionName);
        var document = await collection.Find(u => u.Id == result.Value.UserId).FirstOrDefaultAsync();
        Assert.NotNull(document);
        Assert.Equal(mixedCaseEmail.Trim().ToLowerInvariant(), document.Email);
    }

    [Fact]
    public async Task SignUp_ShouldReturnAConflict_WhenTheEmailIsAlreadyRegistered()
    {
        // Arrange
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        await fixture.RequestReplyBridge.SendAndAwaitReply<SignUpReply>(
            new SignUp(email, "correct horse battery staple", "First User"));

        // Act
        var result = await fixture.RequestReplyBridge.SendAndAwaitReply<SignUpReply>(
            new SignUp(email, "another password entirely", "Second User"));

        // Assert — GL-2: "duplicate email... produce proper error states, not generic 500s".
        Assert.True(result.IsFailure);
        Assert.Equal("identity.email_already_registered", result.Error.Code);
        Assert.Equal(ErrorKind.Conflict, result.Error.Kind);
    }

    [Fact]
    public async Task SignUp_ShouldReturnAConflict_WhenTheEmailDiffersOnlyByCase()
    {
        // Arrange — the whole point of Email's normalization: the unique index only enforces
        // "one account per address" if two differently-cased spellings collide on the same key.
        var localPart = $"case-{Guid.NewGuid():N}";
        await fixture.RequestReplyBridge.SendAndAwaitReply<SignUpReply>(
            new SignUp($"{localPart}@example.com", "correct horse battery staple", "First User"));

        // Act
        var result = await fixture.RequestReplyBridge.SendAndAwaitReply<SignUpReply>(
            new SignUp($"{localPart.ToUpperInvariant()}@EXAMPLE.com", "another password entirely", "Second User"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("identity.email_already_registered", result.Error.Code);
    }

    [Fact]
    public async Task SignUp_ShouldReturnAValidationError_WhenTheEmailIsMalformed()
    {
        // Arrange
        var command = new SignUp("not-an-email", "correct horse battery staple", "Ada Lovelace");

        // Act
        var result = await fixture.RequestReplyBridge.SendAndAwaitReply<SignUpReply>(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("identity.email_invalid", result.Error.Code);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
    }

    [Fact]
    public async Task SignUp_ShouldReturnAValidationError_WhenThePasswordIsTooShort()
    {
        // Arrange
        var command = new SignUp($"short-{Guid.NewGuid():N}@example.com", "short", "Ada Lovelace");

        // Act
        var result = await fixture.RequestReplyBridge.SendAndAwaitReply<SignUpReply>(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("identity.password_too_short", result.Error.Code);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
    }
}
