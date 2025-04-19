using Microsoft.EntityFrameworkCore;
using Repository.Interfaces;
using Repository.Models;

namespace Repository.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly MusicShopDBContext _context;

        public CartRepository(MusicShopDBContext context)
        {
            _context = context;
        }

        public async Task<Cart> GetOrCreateCartAsync(Guid userId)
        {
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }
            return cart;
        }

        public async Task<List<CartItem>> GetCartItemsAsync(Guid cartId)
        {
            return await _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.CartId == cartId)
                .ToListAsync();
        }

        public async Task AddOrUpdateCartItemAsync(Guid cartId, Guid productId, int quantity)
        {
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cartId && ci.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                _context.CartItems.Update(existingItem);
            }
            else
            {
                var newItem = new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cartId,
                    ProductId = productId,
                    Quantity = quantity,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _context.CartItems.AddAsync(newItem);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> RemoveCartItemAsync(Guid cartItemId)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item == null) return false;
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Product?> GetProductByIdAsync(Guid productId)
        {
            return await _context.Products.FindAsync(productId);
        }

        public async Task SaveProductAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task ClearCartAsync(Guid userId)
        {
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null) return;

            var items = await _context.CartItems.Where(i => i.CartId == cart.Id).ToListAsync();
            _context.CartItems.RemoveRange(items);

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }


    }
}
