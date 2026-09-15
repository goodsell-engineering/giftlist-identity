namespace Identity.Contracts.Users;

/// <summary>The success reply to <see cref="Login"/> — see <see cref="SignUpReply"/> for why failure is no longer here.</summary>
public sealed record LoginReply(Guid UserId, string AccessToken, DateTimeOffset AccessTokenExpiresAt);
