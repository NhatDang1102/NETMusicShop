using Microsoft.EntityFrameworkCore;
using Repository.DTOs;
using Repository.Interfaces;
using Repository.Models;
using Service.Interfaces;

namespace Service.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepo;
        private readonly IAuthRepository _authRepo;

        public CartService(ICartRepository cartRepo, IAuthRepository authRepo)
        {
            _cartRepo = cartRepo;
            _authRepo = authRepo;
        }

        public async Task<List<CartItemDto>> GetCartAsync(string userEmail)
        {
            var user = await _authRepo.GetUserByEmailAsync(userEmail);
            if (user == null) return new();

            var cart = await _cartRepo.GetOrCreateCartAsync(user.Id);
            var items = await _cartRepo.GetCartItemsAsync(cart.Id);

            return items.Select(ci => new CartItemDto
            {
                Id = ci.Id,
                Quantity = ci.Quantity,
                ProductId = ci.ProductId ?? Guid.Empty,
                ProductName = ci.Product?.Name,
                ProductPrice = ci.Product?.Price ?? 0,
                ProductImage = ci.Product?.ImageUrl
            }).ToList();
        }

        public async Task AddToCartAsync(string userEmail, Guid productId, int quantity)
        {
            var user = await _authRepo.GetUserByEmailAsync(userEmail);
            if (user == null) throw new Exception("Ko thay user");

            var cart = await _cartRepo.GetOrCreateCartAsync(user.Id);
            await _cartRepo.AddOrUpdateCartItemAsync(cart.Id, productId, quantity);
        }
    }
}
