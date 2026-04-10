using Dapper;
using Npgsql;

namespace RobertDoisneau.Cart.WebApi.Services;

public class CartService : ICartService
{
    private readonly string _connectionString;
    private readonly ILogger<CartService> _logger;

    public CartService(IConfiguration configuration, ILogger<CartService> logger)
    {
        _connectionString = configuration.GetConnectionString("db") ?? throw new Exception("Couldn't connect to db");
        _logger = logger;
    }

    // Recupera i biglietti dell'utente
    public async Task<int> GetTotalTicketsAsync(int userId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT COALESCE(SUM(quantity), 0) 
            FROM public.cart 
            WHERE user_id = @UserId;";

        return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    // 2. Aggiunge al carrello (con logica Upsert: se esiste già, aggiorna la quantità)
    public async Task<bool> AddToCartAsync(int userId, int exhibitionId, int quantity, DateTime date)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO public.cart (user_id, exhibition_id, quantity, date, added_at)
            VALUES (@UserId, @ExhibitionId, @Quantity, @Date, CURRENT_TIMESTAMP)
            ON CONFLICT (utente_id, mostra_id, data_visita) 
            DO UPDATE SET 
                quantity = public.cart.quantity + EXCLUDED.quantity,
                added_at = CURRENT_TIMESTAMP;";

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            UserId = userId,
            ExhibitionId = exhibitionId,
            Quantity = quantity,
            Date = date.Date
        });

        return rowsAffected > 0;
    }

    // 3. Recupera i dettagli per la visualizzazione (Join con Mostre)
    public async Task<IEnumerable<dynamic>> GetUserCartDetailsAsync(int userId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT c.id, m.title, m.price, c.quantity, c.date,
                   (m.price * c.quantity) as subtotal
            FROM public.cart c
            JOIN public.exhibitions m ON c.exhibition_id = m.id
            WHERE c.user_id = @UserId;";

        return await connection.QueryAsync(sql, new { UserId = userId });
    }

    // 4. Metodo per il Background Service di pulizia
    public async Task<int> CleanupExpiredCartsAsync(int minutes)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            DELETE FROM public.cart 
            WHERE added_at < (CURRENT_TIMESTAMP - (@Minutes || ' minutes')::interval)";

        return await connection.ExecuteAsync(sql, new { Minutes = minutes });
    }
}
