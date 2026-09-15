namespace Identity.Contracts.Users;

/// <summary>
/// The success reply to <see cref="SignUp"/>: a fresh access token. Failure no longer travels
/// through this type — a handler that fails replies with
/// <c>BuildingBlocks.Messaging.RequestReply.ReplyFault</c> instead, and the request/reply bridge
/// surfaces it as <c>Result&lt;SignUpReply&gt;.Failure</c>, so a caller unpacks exactly one shape
/// (ARCHITECTURE.md "Command → event flow") rather than a success flag on every reply type as well.
/// </summary>
public sealed record SignUpReply(Guid UserId, string AccessToken, DateTimeOffset AccessTokenExpiresAt);
