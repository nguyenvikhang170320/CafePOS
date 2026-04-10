using CafePos.Data;
using CafePos.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CafePos.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly CafePosDbContext _context;

        public CategoryService(CafePosDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Category>> GetAllActiveCategoriesAsync()
        {
            return await _context.Categories.Where(c => c.IsActive).ToListAsync();
        }

        public async Task<Category> GetCategoryByIdAsync(int id)
        {
            return await _context.Categories.FindAsync(id);
        }

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task SoftDeleteCategoryAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                // Vẫn tuân thủ nguyên tắc không xóa cứng
                category.IsActive = false;
                _context.Categories.Update(category);
                await _context.SaveChangesAsync();
            }
        }
    }
}