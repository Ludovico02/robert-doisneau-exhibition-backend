using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;
using Xunit;

namespace RobertDoisneau.Login.WebApi.Tests;

[Collection("Login API database")]
public sealed class AuthEndpointsIntegrationTests : IDisposable
{
    private readonly LoginApiFactory _factory;

    public AuthEndpointsIntegrationTests(LoginPostgresFixture database)
    {
        _factory = new LoginApiFactory(database.ConnectionString);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Register_ReturnsSuccessForValidUser()
    {
        // CreateClient uses an in-memory HttpClient connected to ASP.NET Core's TestServer.
        // Requests still traverse the real routing, middleware, and endpoint code, but no
        // network port or separately running development server is involved.
        using var client = _factory.CreateClient();
        var username = $"user{Guid.NewGuid():N}"[..12];
        var email = $"{Guid.NewGuid():N}@example.com";
        using var content = JsonContent.Create(new
        {
            username,
            email,
            password = "StrongPass123!"
        });

        var response = await client.PostAsync("/api/auth/register", content);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Registration completed successfully!", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_AttachesHttpOnlyAccessTokenCookie()
    {
        using var client = _factory.CreateClient();
        var username = $"user{Guid.NewGuid():N}"[..12];
        var email = $"{Guid.NewGuid():N}@example.com";

        using var registration = await client.PostAsync(
            "/api/auth/register",
            JsonContent.Create(new
            {
                username,
                email,
                password = "StrongPass123!"
            }));

        Assert.Equal(System.Net.HttpStatusCode.OK, registration.StatusCode);

        using var login = await client.PostAsync(
            "/api/auth/login",
            JsonContent.Create(new
            {
                username,
                password = "StrongPass123!"
            }));

        Assert.Equal(System.Net.HttpStatusCode.OK, login.StatusCode);
        Assert.Contains(login.Headers.GetValues("Set-Cookie"), cookie =>
            cookie.StartsWith("X-Access-Token=", StringComparison.Ordinal) &&
            cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Supplies an isolated PostgreSQL database for the API tests.
/// </summary>
public sealed class LoginPostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            CREATE TABLE public.users
            (
                id SERIAL PRIMARY KEY,
                username VARCHAR(50) NOT NULL UNIQUE,
                password_hash TEXT NOT NULL,
                email VARCHAR(100) NOT NULL UNIQUE,
                created_at TIMESTAMPTZ NOT NULL
            );
            """,
            connection);
        await command.ExecuteNonQueryAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition("Login API database")]
public sealed class LoginApiDatabaseCollection : ICollectionFixture<LoginPostgresFixture>
{
}

/// <summary>
/// Builds the real Login API pipeline with test-only settings and a disposable database.
/// </summary>
public sealed class LoginApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // WebApplicationFactory boots the app in a test host instead of starting Kestrel
        // as a live server. The app's routing, middleware, and dependency injection still run.
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            // These settings override local secrets and normal appsettings for this test host.
            // They point persistence at a disposable PostgreSQL container and provide a test JWT key.
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:db"] = connectionString,
                ["Jwt:Key"] = "test-only-signing-key-at-least-32-characters-long",
                ["Jwt:Issuer"] = "RobertDoisneauAuth",
                ["Jwt:Audience"] = "RobertDoisneauApp"
            });
        });
    }
}
