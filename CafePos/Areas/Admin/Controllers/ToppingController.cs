using CafePos.Data;
using CafePos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafePos.Controllers.Admin
{
    [Area("Admin")]
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

        // Thùng rác
        public async Task<IActionResult> Trash()
        {
            var toppings = await _context.Toppings
                .Where(t => !t.IsActive)
                .OrderBy(t => t.Name)
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

        // Tạo mới
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Price,IsActive")] Topping topping)
        {
            if (!ModelState.IsValid) return View(topping);

            _context.Toppings.Add(topping);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm topping thành công.";
            return RedirectToAction(nameof(Index));
        }

        // Sửa
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var topping = await _context.Toppings.FindAsync(id);
            if (topping == null) return NotFound();

            return View(topping);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ToppingId,Name,Price,IsActive")] Topping topping)
        {
            if (id != topping.ToppingId) return NotFound();

            if (!ModelState.IsValid) return View(topping);

            var toppingInDb = await _context.Toppings.FindAsync(id);
            if (toppingInDb == null) return NotFound();

            toppingInDb.Name = topping.Name;
            toppingInDb.Price = topping.Price;
            toppingInDb.IsActive = topping.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật topping thành công.";
            return RedirectToAction(nameof(Index));
        }

        // Xóa mềm ngay trên Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var topping = await _context.Toppings.FindAsync(id);
            if (topping == null) return NotFound();

            topping.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã chuyển topping vào thùng rác.";
            return RedirectToAction(nameof(Index));
        }

        // Khôi phục từ thùng rác
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var topping = await _context.Toppings.FindAsync(id);
            if (topping == null) return NotFound();

            topping.IsActive = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Khôi phục topping thành công.";
            return RedirectToAction(nameof(Trash));
        }

        // Xóa vĩnh viễn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePermanent(int id)
        {
            var topping = await _context.Toppings.FindAsync(id);
            if (topping == null) return NotFound();

            _context.Toppings.Remove(topping);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa vĩnh viễn topping.";
            return RedirectToAction(nameof(Trash));
        }
    }
}