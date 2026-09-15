using BuildingBlocks.Results;
using Identity.Application.Users;
using Identity.Domain.Users;
using MongoDB.Driver;

namespace Identity.Infrastructure.Users.Persistence;

internal sealed class UserRepository : IUserRepository
{
    public const string CollectionName = "users";

    private readonly IMongoCollection<UserDocument> _users;

    public UserRepository(IMongoDatabase database)
    {
        _users = database.GetCollection<UserDocument>(CollectionName);
    }

    public async Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken)
    {
        var document = await _users
            .Find(u => u.Email == email.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return document is null ? null : UserToDocumentMapper.ToAggregate(document);
    }

    public async Task<Result> AddAsync(User user, CancellationToken cancellationToken)
    {
        var document = UserToDocumentMapper.ToDocument(user);

        try
        {
            await _users.InsertOneAsync(document, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // Expected outcome, not a bug — the unique index caught a race the upfront
            // FindByEmailAsync check in the interactor didn't (CONVENTIONS.md "Errors"). Publishing
            // domain events is not this port's job; see IUserEventPublisher (ARCHITECTURE.md "Domain events are not integration events").
            return UserErrors.EmailAlreadyRegistered;
        }

        return Result.Success();
    }

    /// <summary>
    /// The uniqueness guarantee behind "email already taken" (ARCHITECTURE.md "Data model") — a correctness
    /// requirement, not an optimisation. Applied at startup by
    /// <c>IdentityInfrastructureServiceCollectionExtensions.EnsureIndexesAsync</c>, not left to be
    /// inferred from application code (CONVENTIONS.md "Persistence").
    /// </summary>
    public static Task EnsureIndexesAsync(IMongoDatabase database, CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<UserDocument>(CollectionName);
        var emailIndex = new CreateIndexModel<UserDocument>(
            Builders<UserDocument>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true, Name = "email_unique" });

        return collection.Indexes.CreateOneAsync(emailIndex, cancellationToken: cancellationToken);
    }
}
