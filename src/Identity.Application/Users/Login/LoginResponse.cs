namespace Identity.Application.Users.Login;

public sealed record LoginResponse(Guid UserId, string AccessToken, DateTimeOffset AccessTokenExpiresAt);
