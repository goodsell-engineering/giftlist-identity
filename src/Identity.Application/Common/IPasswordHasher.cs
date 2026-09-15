namespace Identity.Application.Common;

/// <summary>
/// Genuinely domain-agnostic port (CONVENTIONS.md "Folder structure") over the hashing algorithm (BCrypt/Argon2
/// — an infrastructure choice, IMPLEMENTATION_PLAN.md "Phase 1"). Application never sees a hashing library
/// by name.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string plainPassword);

    bool Verify(string plainPassword, string passwordHash);
}
