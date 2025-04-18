using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repository.DTOs;
using Repository.Interfaces;
using Repository.Models;
using Repository.Repositories;
using Service.Interfaces;

namespace Service.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;
        private readonly IImageUploadService _imageUploadService;

        public ProductService(IProductRepository repo, IImageUploadService imageUploadService)
        {
            _repo = repo;
            _imageUploadService = imageUploadService;
        }

        public async Task<List<Product>> GetAllAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<Product> GetByIdAsync(Guid id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task<Product> CreateAsync(ProductCreateDto dto)
        {
            var imageUrl = await _imageUploadService.UploadImageAsync(dto.Image);

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock,
                ImageUrl = imageUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repo.CreateAsync(product);
            return product;
        }

        public async Task<Product> UpdateAsync(Guid id, ProductUpdateDto dto)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product == null) return null;

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            product.UpdatedAt = DateTime.UtcNow;

            if (dto.Image != null)
            {
                var newUrl = await _imageUploadService.UploadImageAsync(dto.Image);
                product.ImageUrl = newUrl;
            }

            await _repo.UpdateAsync(product);
            return product;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product == null) return false;

            return await _repo.RemoveAsync(product);
        }
    }
}
