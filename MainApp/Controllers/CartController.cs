using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.DTOs;
using Service.Interfaces;
using System.Security.Claims;

namespace MainApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "customer")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var items = await _cartService.GetCartAsync(email);
            return Ok(items);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] CartAddDto dto)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            await _cartService.AddToCartAsync(email, dto.ProductId, dto.Quantity);
            return Ok(new { message = "Đã thêm vào giỏ hàng." });
        }

    }
}
