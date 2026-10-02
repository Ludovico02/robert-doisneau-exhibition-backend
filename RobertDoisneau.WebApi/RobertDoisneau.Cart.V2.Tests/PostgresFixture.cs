using Dapper;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace RobertDoisneau.Cart.V2.Tests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        var schema = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "db", "schema.sql"));
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(schema, connection);
        await command.ExecuteNonQueryAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition("db")]
public sealed class DbCollection : ICollectionFixture<PostgresFixture>
{
}
