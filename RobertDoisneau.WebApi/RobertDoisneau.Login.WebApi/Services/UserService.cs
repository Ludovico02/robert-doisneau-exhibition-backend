using Dapper;
using Microsoft.VisualBasic;
using Npgsql;
using RobertDoisneau.Login.WebApi.Models;

namespace RobertDoisneau.Login.WebApi.Services;

public class UserService
{
    private readonly string _connectionString;
    private readonly ILogger<UserService> _logger;

    public UserService(IConfiguration configuration, ILogger<UserService> logger)
    {
        _connectionString = configuration.GetConnectionString("db") ?? throw new Exception("Couldn't connect to db");
        _logger = logger;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Filtriamo direttamente nel database tramite la clausola WHERE
        const string query = """
        SELECT 
            id, 
            username, 
            password_hash as PasswordHash, 
            email, 
            creation_date as CreationDate
        FROM public.utenti
        WHERE username = @Username;
        """;

        return await connection.QuerySingleOrDefaultAsync<User>(query, new { Username = username });
    }

    public async Task<User?> GetByEmailAsync(string username)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Filtriamo direttamente nel database tramite la clausola WHERE
        const string query = """
        SELECT 
            id, 
            username, 
            password_hash as PasswordHash, 
            email, 
            creation_date as CreationDate
        FROM public.utenti
        WHERE email = @Email;
        """;

        return await connection.QuerySingleOrDefaultAsync<User>(query, new { Username = username });
    }

    public async Task AddUserAsync(User newUser)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string query = """
            INSERT INTO public.utenti
            (username, password_hash, email, creation_date)
            VALUES(@Username, @PasswordHash, @Email, @CreationDate)
            RETURNING id;
            """;


        int newId = await connection.QuerySingleAsync<int>(query, newUser);
        newUser.Id = newId;
    }
}
