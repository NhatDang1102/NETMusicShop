using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repository.DTOs;
using Repository.Models;

namespace Service.Interfaces
{
    public interface IProductService
    {
        Task<List<Product>> GetAllAsync();
        Task<Product> GetByIdAsync(Guid id);
        Task<Product> CreateAsync(ProductCreateDto dto);
        Task<Product> UpdateAsync(Product product);
        Task<bool> DeleteAsync(Guid id);
    }
}
