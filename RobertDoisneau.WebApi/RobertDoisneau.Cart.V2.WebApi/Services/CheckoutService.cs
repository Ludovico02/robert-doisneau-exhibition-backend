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
                // 1. Leggiamo la disponibilità e BLOCCHIAMO LA RIGA con FOR UPDATE
                var checkSql = @"
                    SELECT e.id as exhibition_id, e.availability, tc.price 
                    FROM ticket_categories tc
                    JOIN exhibitions e ON tc.exhibition_id = e.id
                    WHERE tc.id = @CategoryId
                    FOR UPDATE OF e;"; // Lock critico per le Race Conditions

                var categoryInfo = await connection.QuerySingleOrDefaultAsync<CategoryCheckInfo>(
                                checkSql, new { CategoryId = item.TicketCategoryId }, transaction);

                if (categoryInfo == null || categoryInfo.Availability < item.Quantity)
                {
                    throw new Exception("Posti esauriti per questa categoria.");
                }

                // 2. Scaliamo la disponibilità
                var updateSql = "UPDATE exhibitions SET availability = availability - @Quantity WHERE id = @ExhibitionId";
                await connection.ExecuteAsync(
                    updateSql,
                    new { Quantity = item.Quantity, ExhibitionId = categoryInfo.ExhibitionId },
                    transaction);

                // 1. Usiamo direttamente il tuo modello
                var ticketsToInsert = new List<PurchasedTicket>();

                for (int i = 0; i < item.Quantity; i++)
                {
                    ticketsToInsert.Add(new PurchasedTicket
                    {
                        // L'Id (Guid) non lo valorizziamo se nel DB hai impostato gen_random_uuid() come default,
                        // altrimenti puoi fare: Id = Guid.NewGuid(),

                        UserId = userId,
                        TicketCategoryId = item.TicketCategoryId, // Corrisponde alla tua proprietà
                        UniqueCode = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper(),
                        PricePaid = categoryInfo.Price,
                        PurchaseDate = DateTime.UtcNow // Aggiungiamo anche questa visto che c'è nel modello
                    });
                }

                // 2. I parametri @ devono combaciare con le proprietà del tuo modello PurchasedTicket
                var insertSql = @"
                    INSERT INTO purchased_tickets (user_id, ticket_category_id, unique_code, price_paid, purchase_date) 
                    VALUES (@UserId, @TicketCategoryId, @UniqueCode, @PricePaid, @PurchaseDate);";

                await connection.ExecuteAsync(insertSql, ticketsToInsert, transaction);
            }

            // Tutto è andato a buon fine, salviamo!
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            // In caso di errore (es. posti insufficienti o crash), annulla tutte le query precedenti
            await transaction.RollbackAsync();
            Console.WriteLine(ex.Message); // Log dell'errore
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
