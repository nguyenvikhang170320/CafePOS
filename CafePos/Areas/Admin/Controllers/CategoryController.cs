using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafePos.Data;
using CafePos.Models;
using Microsoft.AspNetCore.Authorization;

namespace CafePos.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly CafePosDbContext _context;

        public CategoryController(CafePosDbContext context)
        {
            _context = context;
        }

        // CẢ ADMIN VÀ STAFF ĐỀU XEM ĐƯỢC
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.Include(c => c.Products).Where(c => c.IsActive == true).ToListAsync();
            return View(categories);
        }

        // CHỈ ADMIN MỚI ĐƯỢC VÀO TRANG THÊM/SỬA/XÓA
        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken] // Nên thêm dòng này để chống giả mạo request
        public async Task<IActionResult> Create([Bind("Name,IsActive")] Category category)
        {
            // Lúc này category chỉ có Name và IsActive là có dữ liệu từ Form
            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // 1. GET: Admin/Category/Details/5
        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories.Include(c => c.Products)
                .FirstOrDefaultAsync(m => m.CategoryId == id && m.IsActive == true); // Thêm điều kiện IsActive

            if (category == null) return NotFound();

            return View(category);
        }
        // 2. GET: Admin/Category/Edit/5
        [Authorize(Roles = "Admin")] // Chỉ Admin mới được sửa
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            return View(category);
        }

        // 3. POST: Admin/Category/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        // Thêm IsActive vào Bind
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,Name,IsActive")] Category category)
        {
            if (id != category.CategoryId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật danh mục thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Categories.Any(e => e.CategoryId == category.CategoryId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                // Thay vì xóa, ta cập nhật trạng thái
                category.IsActive = false;
                _context.Update(category);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã ẩn danh mục thành công!";
            }

            return RedirectToAction(nameof(Index));
        }


        // 1. Trang danh sách các mục đã xóa mềm
        public async Task<IActionResult> Trash()
        {
            var trashList = await _context.Categories
                                          .Where(c => c.IsActive == false)
                                          .ToListAsync();
            return View(trashList);
        }

        // 2. Hàm khôi phục (Restore)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            category.IsActive = true; // Kích hoạt lại
            _context.Update(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã khôi phục danh mục thành công!";
            return RedirectToAction(nameof(Trash)); // Ở lại trang thùng rác để xem tiếp
        }
    }
}