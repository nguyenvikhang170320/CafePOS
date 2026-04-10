using CafePos.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CafePos.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetAllActiveCategoriesAsync();
        Task<Category> GetCategoryByIdAsync(int id);
        Task<Category> CreateCategoryAsync(Category category);
        Task UpdateCategoryAsync(Category category);
        Task SoftDeleteCategoryAsync(int id);
    }
}