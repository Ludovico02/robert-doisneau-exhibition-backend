using Dapper;
using Npgsql;
using RobertDoisneau.Cart.V2.WebApi.Models;

namespace RobertDoisneau.Cart.V2.WebApi.Services;

public class CheckoutService
{
    private readonly string _connectionString;
    private readonly ILogger<CheckoutService> _logger;

    public CheckoutService(IConfiguration configuration, ILogger<CheckoutService> logger)
    {
        _connectionString = configuration.GetConnectionString("db") ?? throw new Exception("db null");
        _logger = logger;
    }

    public async Task<bool> ProcessPurchaseAsync(int userId, List<CheckoutItem> items)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            foreach (var item in items)
            {
                var checkSql = @"
                    SELECT id AS ExhibitionId, availability, price 
                    FROM exhibitions
                    WHERE id = @CategoryId
                    FOR UPDATE;";

                var categoryInfo = await connection.QuerySingleOrDefaultAsync<CategoryCheckInfo>(
                                checkSql, new { CategoryId = item.TicketCategoryId }, transaction);

                if (categoryInfo == null || categoryInfo.Availability < item.Quantity)
                {
                    throw new Exception($"Posti esauriti o categoria non trovata per l'ID: {item.TicketCategoryId}.");
                }

                var updateSql = """
                    UPDATE exhibitions 
                    SET availability = availability - @Quantity 
                    WHERE id = @CategoryId;
                    """;

                await connection.ExecuteAsync(
                    updateSql,
                    new { Quantity = item.Quantity, CategoryId = categoryInfo.ExhibitionId },
                    transaction);

                var ticketsToInsert = new List<PurchasedTicket>();

                for (int i = 0; i < item.Quantity; i++)
                {
                    ticketsToInsert.Add(new PurchasedTicket
                    {
                        UserId = userId,
                        ExhibitionId = categoryInfo.ExhibitionId,
                        UniqueCode = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper(),
                        PricePaid = categoryInfo.Price,
                        PurchaseDate = DateTime.UtcNow
                    });
                }

                var insertSql = @"
                    INSERT INTO purchased_tickets (user_id, exhibition_id, unique_code, price_paid, purchase_date) 
                    VALUES (@UserId, @ExhibitionId, @UniqueCode, @PricePaid, @PurchaseDate);";

                await connection.ExecuteAsync(insertSql, ticketsToInsert, transaction);
            }

            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(ex, "Errore durante la transazione di checkout per l'utente {UserId}", userId);

            return false;
        }
    }
}

public class CategoryCheckInfo
{
    public int ExhibitionId { get; set; }
    public int Availability { get; set; }
    public decimal Price { get; set; }
}