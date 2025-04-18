using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repository.Models;

namespace Repository.Interfaces
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllAsync();
        Task<Product> GetByIdAsync(Guid id);
        Task<int> CreateAsync(Product product);
        Task<int> UpdateAsync(Product product);
        Task<bool> RemoveAsync(Product product);
    }
}
