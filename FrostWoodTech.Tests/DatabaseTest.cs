namespace FrostWoodTech.Tests;

/// <summary>Empties the shared database before each test, so no test inherits rows another one left.</summary>
[Collection(nameof(PostgresCollection))]
public abstract class DatabaseTest : IAsyncLifetime
{
    protected readonly PostgresFixture _fixture;

    protected DatabaseTest(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
