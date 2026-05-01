using CafePos.Data;
using CafePos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafePos.Controllers
{
    
    public class ToppingController : Controller
    {
        private readonly CafePosDbContext _context;

        public ToppingController(CafePosDbContext context)
        {
            _context = context;
        }

        // Danh sách đang hoạt động
        public async Task<IActionResult> Index()
        {
            var toppings = await _context.Toppings
                .Where(t => t.IsActive)
                .OrderBy(t => t.ToppingId)
                .ToListAsync();

            return View(toppings);
        }

       

        // Chi tiết
        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null) return NotFound();

            var topping = await _context.Toppings
                .FirstOrDefaultAsync(t => t.ToppingId == id);

            if (topping == null) return NotFound();

            return View(topping);
        }
    }
}