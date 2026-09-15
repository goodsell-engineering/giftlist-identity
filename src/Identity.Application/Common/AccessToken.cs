namespace Identity.Application.Common;

/// <summary>A minted, ready-to-hand-back access token (CONVENTIONS.md "Domain modelling" — no primitive obsession).</summary>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
