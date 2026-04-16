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
                // 1. Controllo disponibilità con blocco della riga (FOR UPDATE)
                // Usiamo l'alias "AS ExhibitionId" e il parametro @CategoryId
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

                // 2. Aggiornamento della disponibilità
                var updateSql = """
                    UPDATE exhibitions 
                    SET availability = availability - @Quantity 
                    WHERE id = @CategoryId;
                    """;

                await connection.ExecuteAsync(
                    updateSql,
                    new { Quantity = item.Quantity, CategoryId = categoryInfo.ExhibitionId },
                    transaction);

                // 3. Preparazione dei biglietti da inserire
                var ticketsToInsert = new List<PurchasedTicket>();

                for (int i = 0; i < item.Quantity; i++)
                {
                    ticketsToInsert.Add(new PurchasedTicket
                    {
                        UserId = userId,
                        ExhibitionId = categoryInfo.ExhibitionId, // NUOVO: Collegamento con la tabella exhibitions
                        UniqueCode = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper(),
                        PricePaid = categoryInfo.Price,
                        PurchaseDate = DateTime.UtcNow
                    });
                }

                // 4. Inserimento nel database (inclusa la colonna exhibition_id)
                var insertSql = @"
                    INSERT INTO purchased_tickets (user_id, exhibition_id, unique_code, price_paid, purchase_date) 
                    VALUES (@UserId, @ExhibitionId, @UniqueCode, @PricePaid, @PurchaseDate);";

                await connection.ExecuteAsync(insertSql, ticketsToInsert, transaction);
            }

            // Se tutto il ciclo finisce senza errori, confermiamo la transazione
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            // In caso di errore annulla tutte le query precedenti per evitare dati parziali
            await transaction.RollbackAsync();

            // Logghiamo l'errore reale invece di fare solo Console.WriteLine
            _logger.LogError(ex, "Errore durante la transazione di checkout per l'utente {UserId}", userId);

            return false;
        }
    }
}

// Classe di supporto per mappare i dati in lettura
public class CategoryCheckInfo
{
    public int ExhibitionId { get; set; }
    public int Availability { get; set; }
    public decimal Price { get; set; }
}