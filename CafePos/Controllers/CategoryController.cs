using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafePos.Data;
using CafePos.Models;
using Microsoft.AspNetCore.Authorization;

namespace CafePos.Controllers
{
    
    public class CategoryController : Controller
    {
        private readonly CafePosDbContext _context;

        public CategoryController(CafePosDbContext context)
        {
            _context = context;
        }

        // CẢ ADMIN VÀ STAFF ĐỀU XEM ĐƯỢC
        [Authorize(Roles = "Admin,Staff,Employee")]
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.Include(c => c.Products).OrderBy(c => c.CategoryId).ToListAsync();
            return View(categories);
        }

        // 1. GET: Admin/Category/Details/5
        [Authorize(Roles = "Admin,Staff,Employee")] // Cả 2 đều xem được chi tiết
        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories.Include(c => c.Products)
                .FirstOrDefaultAsync(m => m.CategoryId == id && m.IsActive == true); // Thêm điều kiện IsActive

            if (category == null) return NotFound();

            return View(category);
        }
    }
}