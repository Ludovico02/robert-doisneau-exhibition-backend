using Dapper;
using Npgsql;
using RobertDoisneau.Cart.V2.WebApi.Models;
using System.Security.Cryptography;

namespace RobertDoisneau.Cart.V2.WebApi.Services;

public class CheckoutService
{
    private const string LockExhibitionSql = """
        SELECT id AS ExhibitionId, availability, price
        FROM public.exhibitions
        WHERE id = @ExhibitionId
        FOR UPDATE;
        """;

    private const string DecrementStockSql = """
        UPDATE public.exhibitions
        SET availability = availability - @Quantity
        WHERE id = @ExhibitionId AND availability >= @Quantity;
        """;

    private const string InsertTicketSql = """
        INSERT INTO public.purchased_tickets (user_id, exhibition_id, unique_code, price_paid, purchase_date)
        VALUES (@UserId, @ExhibitionId, @UniqueCode, @PricePaid, @PurchaseDate);
        """;

    private readonly string _connectionString;
    private readonly ILogger<CheckoutService> _logger;

    public CheckoutService(IConfiguration configuration, ILogger<CheckoutService> logger)
    {
        _connectionString = configuration.GetConnectionString("db")
            ?? throw new InvalidOperationException("Connection string 'db' is missing.");
        _logger = logger;
    }

    public async Task<PurchaseResult> ProcessPurchaseAsync(int userId, IEnumerable<CheckoutItem>? items)
    {
        if (!CheckoutRules.TryNormalize(items, out var order, out _))
            return PurchaseResult.InvalidRequest;

        await using var connection = new NpgsqlConnection(_connectionString);
        NpgsqlTransaction? transaction = null;

        try
        {
            await connection.OpenAsync();
            transaction = await connection.BeginTransactionAsync();

            foreach (var item in order)
            {
                var stock = await connection.QuerySingleOrDefaultAsync<ExhibitionStock>(
                    LockExhibitionSql, new { ExhibitionId = item.TicketCategoryId }, transaction);

                if (stock is null)
                {
                    await transaction.RollbackAsync();
                    return PurchaseResult.NotFound;
                }

                if (stock.Availability < item.Quantity)
                {
                    await transaction.RollbackAsync();
                    return PurchaseResult.SoldOut;
                }

                var updatedRows = await connection.ExecuteAsync(
                    DecrementStockSql,
                    new { item.Quantity, ExhibitionId = stock.ExhibitionId },
                    transaction);
                if (updatedRows != 1)
                {
                    throw new InvalidOperationException(
                        $"Stock update affected {updatedRows} rows for exhibition {stock.ExhibitionId}.");
                }

                var tickets = new List<PurchasedTicket>(item.Quantity);
                for (var i = 0; i < item.Quantity; i++)
                {
                    tickets.Add(new PurchasedTicket
                    {
                        UserId = userId,
                        ExhibitionId = stock.ExhibitionId,
                        UniqueCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(5)),
                        PricePaid = stock.Price,
                        PurchaseDate = DateTime.UtcNow
                    });
                }

                await connection.ExecuteAsync(InsertTicketSql, tickets, transaction);
            }

            await transaction.CommitAsync();
            return PurchaseResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Checkout transaction failed for user {UserId}", userId);
            if (transaction is not null)
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogWarning(rollbackEx, "Rollback failed for user {UserId}", userId);
                }
            }

            return PurchaseResult.Error;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }
}

public class ExhibitionStock
{
    public int ExhibitionId { get; set; }
    public int Availability { get; set; }
    public decimal Price { get; set; }
}
