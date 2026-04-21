using Dapper;
using Npgsql;
using RobertDoisneau.Cart.V2.WebApi.Models;

namespace RobertDoisneau.Cart.V2.WebApi.Services;

public class ExhibitionService
{
    private readonly string _connectionString;
    private readonly ILogger<ExhibitionService> _logger;

    public ExhibitionService(IConfiguration configuration, ILogger<ExhibitionService> logger)
    {
        _connectionString = configuration.GetConnectionString("db") ?? throw new Exception("db null");
        _logger = logger;
    }

    public async Task<IEnumerable<Exhibition>> GetListAsync()
    {
        using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string selectionQuery = """
            SELECT 
            id as Id,
            title as Title,
            description as Description,
            description2 as Description2,
            description3 as Description3,
            price as Price,
            total_capacity as TotalCapacity,
            availability as Availability
            FROM public.exhibitions
            ORDER BY price ASC
            """;

        var result = await connection.QueryAsync<Exhibition>(selectionQuery);

        return result;
    }
}
