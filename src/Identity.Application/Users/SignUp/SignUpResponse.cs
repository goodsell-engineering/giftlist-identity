namespace Identity.Application.Users.SignUp;

public sealed record SignUpResponse(Guid UserId, string AccessToken, DateTimeOffset AccessTokenExpiresAt);
