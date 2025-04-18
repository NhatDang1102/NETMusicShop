using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusicShop.Repositories.Base;
using Repository.Interfaces;
using Repository.Models;

namespace Repository.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(MusicShopDBContext context) : base(context)
        {
            
        }
    }
}
