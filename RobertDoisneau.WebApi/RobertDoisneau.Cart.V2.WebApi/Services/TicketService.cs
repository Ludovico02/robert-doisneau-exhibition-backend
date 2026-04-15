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
                id, 
                user_id, 
                unique_code, 
                price_paid, 
                purchase_date
            FROM purchased_tickets
            WHERE user_id = @UserId
            ORDER BY purchase_date DESC;";

        using var connection = new NpgsqlConnection(_connectionString);

        return await connection.QueryAsync<PurchasedTicket>(sql, new { UserId = userId });
    }
}
