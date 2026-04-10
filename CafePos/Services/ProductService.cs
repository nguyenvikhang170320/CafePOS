using CafePos.Data;
using CafePos.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CafePos.Services
{
    public class ProductService : IProductService
    {
        private readonly CafePosDbContext _context;

        public ProductService(CafePosDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllActiveProductsAsync()
        {
            // Chỉ lấy các sản phẩm đang hoạt động theo đúng chuẩn
            return await _context.Products.Where(p => p.IsActive).ToListAsync();
        }

        public async Task<Product> GetProductByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task UpdateProductAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task SoftDeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
               // KHÔNG XOÁ CỨNG, chỉ cập nhật IsActive = false 
                product.IsActive = false;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();
            }
        }
    }
}