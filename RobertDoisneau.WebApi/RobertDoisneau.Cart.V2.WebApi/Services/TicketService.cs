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
        // Niente più alias. Lasciamo i nomi originali del DB con gli underscore.
        // Dapper farà la magia e li mapperà su UserId e TicketCategoryId.
        var sql = @"
        SELECT 
            id, 
            user_id, 
            ticket_category_id, 
            unique_code, 
            price_paid, 
            purchase_date
        FROM purchased_tickets
        WHERE user_id = @UserId
        ORDER BY purchase_date DESC;";

        using var connection = new NpgsqlConnection(_connectionString);

        // Passiamo il TUO modello a Dapper
        return await connection.QueryAsync<PurchasedTicket>(sql, new { UserId = userId });
    }
}
