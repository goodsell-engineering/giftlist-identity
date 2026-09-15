using Identity.Domain.Users;

namespace Identity.Infrastructure.Users.Persistence;

/// <summary>Source + "To" + target (CONVENTIONS.md "Naming").</summary>
internal static class UserToDocumentMapper
{
    public static UserDocument ToDocument(User user) => new()
    {
        Id = user.Id.Value,
        Email = user.Email.Value,
        PasswordHash = user.PasswordHash.Value,
        DisplayName = user.DisplayName.Value,
        // UserDocument.CreatedAt's own doc comment explains why this is a DateTime, not the
        // aggregate's DateTimeOffset — CreatedAt is always UTC already, so .UtcDateTime is lossless.
        CreatedAt = user.CreatedAt.UtcDateTime,
    };

    public static User ToAggregate(UserDocument document) => User.Rehydrate(
        new UserId(document.Id),
        new Email(document.Email),
        new PasswordHash(document.PasswordHash),
        new DisplayName(document.DisplayName),
        new DateTimeOffset(document.CreatedAt, TimeSpan.Zero));
}
