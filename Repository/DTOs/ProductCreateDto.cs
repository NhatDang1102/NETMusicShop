using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Repository.DTOs
{
    public class ProductCreateDto
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int? Stock { get; set; }
        public IFormFile Image { get; set; }
    }
}
