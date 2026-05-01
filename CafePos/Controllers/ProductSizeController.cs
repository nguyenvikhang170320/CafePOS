using CafePos.Data;
using CafePos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CafePos.Controllers
{
    
    public class ProductSizeController : Controller
    {
        private readonly CafePosDbContext _context;

        public ProductSizeController(CafePosDbContext context)
        {
            _context = context;
        }

        // =========================
        // DANH SÁCH
        // =========================
        public async Task<IActionResult> Index()
        {
            var productSizes = await _context.ProductSizes
                .Where(x => !x.IsDeleted)
                .Include(x => x.Product)
                .OrderBy(x => x.ProductSizeId)
                .ThenBy(x => x.SizeName)
                .ToListAsync();

            return View(productSizes);
        }

        // =========================
        // CHI TIẾT
        // =========================
        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null) return NotFound();

            var productSize = await _context.ProductSizes
                .Where(x => !x.IsDeleted)
                .Include(x => x.Product)
                .FirstOrDefaultAsync(x => x.ProductSizeId == id);

            if (productSize == null) return NotFound();

            return View(productSize);
        }
    }
}