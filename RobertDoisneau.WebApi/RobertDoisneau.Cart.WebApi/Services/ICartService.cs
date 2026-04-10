namespace RobertDoisneau.Cart.WebApi.Services
{
    public interface ICartService
    {
        Task<bool> AddToCartAsync(int userId, int exhibitionId, int quantity, DateTime date);
        Task<int> CleanupExpiredCartsAsync(int minutes);
        Task<int> GetTotalTicketsAsync(int userId);
        Task<IEnumerable<dynamic>> GetUserCartDetailsAsync(int userId);
    }
}