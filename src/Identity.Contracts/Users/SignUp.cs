namespace Identity.Contracts.Users;

/// <summary>
/// Command: ask Identity to create a new account. Sent with <c>bus.Send()</c> and answered with
/// <see cref="SignUpReply"/> via <c>bus.Reply()</c> — one of the three request/reply flows
/// (ARCHITECTURE.md "Command → event flow"), since the caller needs a yes/no (is the email taken?) before it can
/// respond to whoever is waiting.
/// </summary>
public sealed record SignUp(string Email, string Password, string DisplayName);
