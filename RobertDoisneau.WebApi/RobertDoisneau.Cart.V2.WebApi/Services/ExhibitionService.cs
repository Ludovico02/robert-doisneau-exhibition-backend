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
        var sql = @"
            SELECT e.id, e.title, e.description, e.availability,
                   tc.id, tc.exhibition_id, tc.name, tc.price
            FROM exhibitions e
            LEFT JOIN ticket_categories tc ON e.id = tc.exhibition_id
            WHERE e.availability > 0;";

        using var connection = new NpgsqlConnection(_connectionString);

        var exhibitionDictionary = new Dictionary<int, Exhibition>();

        var result = await connection.QueryAsync<Exhibition, TicketCategory, Exhibition>(
            sql,
            (exhibition, category) =>
            {
                if (!exhibitionDictionary.TryGetValue(exhibition.Id, out var currentExhibition))
                {
                    currentExhibition = exhibition;
                    exhibitionDictionary.Add(currentExhibition.Id, currentExhibition);
                }

                if (category != null)
                    currentExhibition.Categories.Add(category);

                return currentExhibition;
            },
            splitOn: "id" // Dapper capisce che qui inizia la seconda tabella
        );

        return exhibitionDictionary.Values;
    }
}
