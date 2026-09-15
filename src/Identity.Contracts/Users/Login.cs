namespace Identity.Contracts.Users;

/// <summary>
/// Command: ask Identity to authenticate a user. Sent with <c>bus.Send()</c> and answered with
/// <see cref="LoginReply"/> via <c>bus.Reply()</c> (ARCHITECTURE.md "Command → event flow" — the caller is waiting
/// on the JWT).
/// </summary>
public sealed record Login(string Email, string Password);
