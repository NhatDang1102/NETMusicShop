using Repository.Models;

namespace Repository.Interfaces
{
    public interface ICartRepository
    {

        Task<Cart> GetOrCreateCartAsync(Guid userId);
        Task<List<CartItem>> GetCartItemsAsync(Guid cartId);
        Task AddOrUpdateCartItemAsync(Guid cartId, Guid productId, int quantity);
        Task<bool> RemoveCartItemAsync(Guid cartItemId);


    }
}
