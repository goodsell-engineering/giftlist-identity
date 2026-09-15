namespace Identity.IntegrationTests.Fixtures;

[CollectionDefinition(Name)]
public sealed class IdentityCollection : ICollectionFixture<IdentityFixture>
{
    public const string Name = "Identity";
}
