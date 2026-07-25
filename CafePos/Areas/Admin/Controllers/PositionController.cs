using CafePos.Data;
using CafePos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafePos.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PositionController : Controller
    {
        private readonly CafePosDbContext _context;

        public PositionController(CafePosDbContext context)
        {
            _context = context;
        }

        // 1. Chỉ lấy danh sách Chức danh CHƯA BỊ XÓA MỀM
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var positions = await _context.Positions
                .Where(p => p.IsActive) // Lọc bỏ các dòng đã xóa mềm
                .ToListAsync();

            return View(positions);
        }
        // 2. Thêm mới (GET)
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // 2. Thêm mới (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Position position)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng tên chức danh
                if (await _context.Positions.AnyAsync(p => p.PositionName == position.PositionName))
                {
                    ModelState.AddModelError("PositionName", "Tên chức danh này đã tồn tại.");
                    return View(position);
                }

                _context.Positions.Add(position);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm chức danh mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(position);
        }

        // 3. Chỉnh sửa (GET)
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var position = await _context.Positions.FindAsync(id);
            if (position == null) return NotFound();

            return View(position);
        }

        // 3. Chỉnh sửa (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Position position)
        {
            if (id != position.PositionId) return NotFound();

            if (ModelState.IsValid)
            {
                // Kiểm tra trùng tên chức danh (trừ chính nó)
                if (await _context.Positions.AnyAsync(p => p.PositionName == position.PositionName && p.PositionId != id))
                {
                    ModelState.AddModelError("PositionName", "Tên chức danh này đã tồn tại.");
                    return View(position);
                }

                try
                {
                    _context.Update(position);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật chức danh thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Positions.AnyAsync(p => p.PositionId == id))
                        return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(position);
        }
        // 4. Hàm Xóa mềm (Soft Delete)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var position = await _context.Positions.FindAsync(id);
            if (position == null) return NotFound();

            // Kiểm tra nếu đang có nhân viên giữ chức danh này
            bool isUsed = await _context.Employees.AnyAsync(e => e.PositionId == id);
            if (isUsed)
            {
                TempData["ErrorMessage"] = "Không thể xóa chức danh này vì đang có nhân viên nắm giữ!";
                return RedirectToAction(nameof(Index));
            }

            // XÓA MỀM: Đánh dấu IsDeleted = true thay vì _context.Positions.Remove(position)
            position.IsActive = false;
            _context.Update(position);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa chức danh thành công!";
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var position = await _context.Positions.FindAsync(id);
            if (position == null) return NotFound();

            position.IsActive = true; // Bật lại trạng thái hoạt động
            _context.Update(position);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã khôi phục chức danh thành công!";
            return RedirectToAction(nameof(Index));
        }

    }
}
