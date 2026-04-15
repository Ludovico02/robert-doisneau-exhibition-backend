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

    public async Task<IEnumerable<Exhibition>> GetAllActiveExhibitionsAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var exhibitionsSql = """
            SELECT e.id, e.title, e.description, e.availability 
            FROM exhibitions e 
            WHERE availability > 0;
            """;
        var exhibitions = (await connection.QueryAsync<Exhibition>(exhibitionsSql)).ToList();

        if (!exhibitions.Any())
            return exhibitions;

        var categoriesSql = """
            SELECT tc.id, tc.exhibition_id, tc.name, tc.price 
            FROM ticket_categories tc;
            """;

        var allCategories = await connection.QueryAsync<TicketCategory>(categoriesSql);

        foreach (var exhibition in exhibitions)
        {
            var exhibitionTickets = allCategories.Where(c => c.ExhibitionId == exhibition.Id).ToList();

            exhibition.Categories.AddRange(exhibitionTickets);
        }

        return exhibitions;
    }

    public async Task<IEnumerable<TicketCategory>> GetTicketCategoriesByExhibitionIdAsync(int exhibitionId)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        var sql = """
        SELECT id, exhibition_id AS ExhibitionId, name, price 
        FROM ticket_categories 
        WHERE exhibition_id = @ExhibitionId;
        """;

        var categories = await connection.QueryAsync<TicketCategory>(sql, new { ExhibitionId = exhibitionId });

        return categories;
    }
}
