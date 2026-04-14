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
                // Leggiamo la disponibilità e BLOCCHIAMO LA RIGA con FOR UPDATE
                // Lo faccio per le Race Conditions (2 utenti che comprano contemporaneamente gli ultimi biglietti)
                var checkSql = @"
                    SELECT e.id as exhibition_id, e.availability, tc.price 
                    FROM ticket_categories tc
                    JOIN exhibitions e ON tc.exhibition_id = e.id
                    WHERE tc.id = @CategoryId
                    FOR UPDATE OF e;";

                var categoryInfo = await connection.QuerySingleOrDefaultAsync<CategoryCheckInfo>(
                                checkSql, new { CategoryId = item.TicketCategoryId }, transaction);

                if (categoryInfo == null || categoryInfo.Availability < item.Quantity)
                {
                    throw new Exception("Posti esauriti per questa categoria.");
                }

                var updateSql = """
                    UPDATE exhibitions 
                    SET availability = availability - @Quantity 
                    WHERE id = @ExhibitionId;
                    """;
                await connection.ExecuteAsync(
                    updateSql,
                    new { Quantity = item.Quantity, ExhibitionId = categoryInfo.ExhibitionId },
                    transaction);

                var ticketsToInsert = new List<PurchasedTicket>();

                for (int i = 0; i < item.Quantity; i++)
                {
                    ticketsToInsert.Add(new PurchasedTicket
                    {
                        UserId = userId,
                        TicketCategoryId = item.TicketCategoryId,
                        UniqueCode = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper(),
                        PricePaid = categoryInfo.Price,
                        PurchaseDate = DateTime.UtcNow
                    });
                }

                var insertSql = @"
                    INSERT INTO purchased_tickets (user_id, ticket_category_id, unique_code, price_paid, purchase_date) 
                    VALUES (@UserId, @TicketCategoryId, @UniqueCode, @PricePaid, @PurchaseDate);";

                await connection.ExecuteAsync(insertSql, ticketsToInsert, transaction);
            }

            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            // In caso di errore annulla tutte le query precedenti
            await transaction.RollbackAsync();
            Console.WriteLine(ex.Message);
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
