using Identity.Application.Common;

namespace Identity.Infrastructure.Platform.Security;

/// <summary>
/// BCrypt over Argon2 (IMPLEMENTATION_PLAN.md "Phase 1" names both as acceptable) — BCrypt.Net-Next needs no
/// native dependency, which matters for a container image that otherwise has none. Genuinely
/// domain-agnostic, so it lives in Platform/ alongside the port it implements (CONVENTIONS.md "Folder structure").
/// </summary>
internal sealed class BCryptPasswordHasher : IPasswordHasher
{
    // 12 is BCrypt's own recommended floor for a service issuing tokens interactively — enough
    // rounds to resist offline cracking without making sign-up/login noticeably slow.
    private const int WorkFactor = 12;

    public string Hash(string plainPassword) =>
        BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: WorkFactor);

    public bool Verify(string plainPassword, string passwordHash) =>
        BCrypt.Net.BCrypt.Verify(plainPassword, passwordHash);
}
