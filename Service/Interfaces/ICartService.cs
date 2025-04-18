using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repository.DTOs;
using Repository.Models;

namespace Service.Interfaces
{
    public interface ICartService
    {
        Task<List<CartItemDto>> GetCartAsync(string userEmail);
        Task AddToCartAsync(string userEmail, Guid productId, int quantity);
    }
}
