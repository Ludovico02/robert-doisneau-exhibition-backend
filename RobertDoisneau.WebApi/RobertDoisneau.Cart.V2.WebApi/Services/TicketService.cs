using Dapper;
using Npgsql;
using RobertDoisneau.Cart.V2.WebApi.Models;

namespace RobertDoisneau.Cart.V2.WebApi.Services;

public class TicketService
{
    private readonly string _connectionString;
    private readonly ILogger<TicketService> _logger;

    public TicketService(IConfiguration configuration, ILogger<TicketService> logger)
    {
        _connectionString = configuration.GetConnectionString("db") ?? throw new Exception("db null");
        _logger = logger;
    }

    public async Task<IEnumerable<PurchasedTicket>> GetUserTicketsAsync(int userId)
    {
        var sql = @"
            SELECT 
                pt.id, 
                pt.user_id, 
                pt.exhibition_id,
                pt.unique_code, 
                pt.price_paid, 
                pt.purchase_date,
                e.title AS ExhibitionTitle  -- Prendiamo il titolo dall'altra tabella
            FROM purchased_tickets pt
            INNER JOIN exhibitions e ON pt.exhibition_id = e.id
            WHERE pt.user_id = @UserId
            ORDER BY pt.purchase_date DESC;";

        using var connection = new NpgsqlConnection(_connectionString);

        return await connection.QueryAsync<PurchasedTicket>(sql, new { UserId = userId });
    }
}
