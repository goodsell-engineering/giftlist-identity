using Identity.Domain.Users;

namespace Identity.Application.Common;

/// <summary>
/// Port over JWT issuance with the asymmetric signing key (ARCHITECTURE.md "Why workers still need a little HTTP" — Identity holds
/// the private key and is the only service that can mint a token). Lives in <c>Common/</c> so
/// the naming-convention text scan does not treat it as a use-case port needing its own
/// Interactor/Request pair — the same reason <see cref="IClock"/> lives here. That placement
/// does not require a domain-agnostic *signature*, though: Application may reference Domain
/// (CONVENTIONS.md "Project reference graph"), so this takes the subject's own value objects rather than primitives.
/// Passing primitives previously let two adjacent <see cref="string"/> parameters compile in the
/// wrong order (an email landing in a display-name claim); <see cref="Email"/>/<see cref="DisplayName"/>
/// make that a compile error instead. Application never sees the key material or a JWT library
/// by name.
/// </summary>
public interface ITokenIssuer
{
    AccessToken Issue(UserId subjectId, Email email, DisplayName displayName);
}
