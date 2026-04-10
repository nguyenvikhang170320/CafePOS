using CafePos.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CafePos.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllActiveProductsAsync();
        Task<Product> GetProductByIdAsync(int id);
        Task<Product> CreateProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task SoftDeleteProductAsync(int id);
    }
}