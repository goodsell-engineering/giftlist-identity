namespace Identity.Infrastructure.Users.Persistence;

/// <summary>
/// The Mongo-facing shape of a user, kept separate from the <c>Identity.Domain.Users.User</c>
/// aggregate (ARCHITECTURE.md "Data that crosses boundaries" — no <c>[Bson*]</c> attributes in Domain). No attributes are
/// needed here either: <c>Id</c> auto-maps to <c>_id</c> by the driver's own default convention,
/// and field names are camelCased by the shared <c>BuildingBlocks.Persistence.MongoConventions</c>
/// pack registered once at startup.
///
/// <see cref="CreatedAt"/> is a <see cref="DateTime"/>, not the aggregate's own
/// <see cref="DateTimeOffset"/>: the Mongo driver's default representation for
/// <see cref="DateTimeOffset"/> is a two-element array (ticks, offset), which does not
/// range-query the way a native BSON date does. <see cref="User.CreatedAt"/> is always UTC (it
/// comes from <c>IClock.UtcNow</c>), so nothing is lost by storing it as one.
/// </summary>
public sealed class UserDocument
{
    public required Guid Id { get; init; }

    public required string Email { get; init; }

    public required string PasswordHash { get; init; }

    public required string DisplayName { get; init; }

    public required DateTime CreatedAt { get; init; }
}
