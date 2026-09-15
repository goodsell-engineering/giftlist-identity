using BuildingBlocks.Results;
using Identity.Contracts.Users;
using Identity.IntegrationTests.Fixtures;

namespace Identity.IntegrationTests.Users;

/// <summary>Enters at the real <c>LoginHandler</c> Rebus handler (GL-2's "log in... through the browser").</summary>
[Collection(IdentityCollection.Name)]
public sealed class LoginTests(IdentityFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_ShouldReturnAnAccessToken_WhenCredentialsAreCorrect()
    {
        // Arrange
        var email = $"login-{Guid.NewGuid():N}@example.com";
        const string password = "correct horse battery staple";
        var signedUp = await fixture.RequestReplyBridge.SendAndAwaitReply<SignUpReply>(
            new SignUp(email, password, "Ada Lovelace"));
        Assert.True(signedUp.IsSuccess);

        // Act
        var result = await fixture.RequestReplyBridge.SendAndAwaitReply<LoginReply>(new Login(email, password));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(signedUp.Value.UserId, result.Value.UserId);
        Assert.NotEmpty(result.Value.AccessToken);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthenticated_WhenThePasswordIsWrong()
    {
        // Arrange — GL-2: "bad credentials produce proper error states, not generic 500s".
        var email = $"wrongpw-{Guid.NewGuid():N}@example.com";
        await fixture.RequestReplyBridge.SendAndAwaitReply<SignUpReply>(
            new SignUp(email, "correct horse battery staple", "Ada Lovelace"));

        // Act
        var result = await fixture.RequestReplyBridge.SendAndAwaitReply<LoginReply>(new Login(email, "totally wrong password"));

        // Assert — Unauthenticated, never Forbidden: "log in again", not "you may not do this".
        Assert.True(result.IsFailure);
        Assert.Equal("identity.invalid_credentials", result.Error.Code);
        Assert.Equal(ErrorKind.Unauthenticated, result.Error.Kind);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthenticated_WhenNoAccountExistsForTheEmail()
    {
        // Arrange
        var email = $"nosuchuser-{Guid.NewGuid():N}@example.com";

        // Act
        var result = await fixture.RequestReplyBridge.SendAndAwaitReply<LoginReply>(new Login(email, "whatever password"));

        // Assert — the same code and kind as a wrong password: telling the two apart would let a
        // caller enumerate registered emails (UserErrors.InvalidCredentials' own doc comment).
        Assert.True(result.IsFailure);
        Assert.Equal("identity.invalid_credentials", result.Error.Code);
        Assert.Equal(ErrorKind.Unauthenticated, result.Error.Kind);
    }

    [Fact]
    public async Task Login_ShouldSucceed_WhenTheEmailDiffersOnlyByCaseFromSignUp()
    {
        // Arrange
        var localPart = $"caselogin-{Guid.NewGuid():N}";
        const string password = "correct horse battery staple";
        await fixture.RequestReplyBridge.SendAndAwaitReply<SignUpReply>(
            new SignUp($"{localPart}@example.com", password, "Ada Lovelace"));

        // Act
        var result = await fixture.RequestReplyBridge.SendAndAwaitReply<LoginReply>(
            new Login($"{localPart.ToUpperInvariant()}@EXAMPLE.com", password));

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Login_ShouldReturnAValidationError_WhenTheEmailIsMalformed()
    {
        // Arrange
        var command = new Login("not-an-email", "whatever password");

        // Act
        var result = await fixture.RequestReplyBridge.SendAndAwaitReply<LoginReply>(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("identity.email_invalid", result.Error.Code);
        Assert.Equal(ErrorKind.Validation, result.Error.Kind);
    }
}
