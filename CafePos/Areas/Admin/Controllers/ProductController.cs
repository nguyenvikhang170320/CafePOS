using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafePos.Data;
using CafePos.Models;
using CafePos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering; // Cần dòng này để dùng SelectList

namespace CafePos.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly CafePosDbContext _context;
        private readonly PhotoService _photoService;
        public ProductController(CafePosDbContext context, PhotoService photoService)
        {
            _context = context;
            _photoService = photoService;
        }

        // 1. INDEX: Hiển thị danh sách sản phẩm đang bán
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Index()
        {
            // Quan trọng: Phải có .Include(p => p.Category) để lấy được tên danh mục
            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive == true)
                .OrderBy(p => p.ProductId)
                .ToListAsync();
            return View(products);
        }

        // 2. CREATE (GET): Hiển thị form thêm mới
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            // Lấy danh sách danh mục đang hoạt động để hiện vào Dropdown
            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(c => c.IsActive), "CategoryId", "Name");
            return View();
        }

        // 3. CREATE (POST): Xử lý lưu sản phẩm
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,CategoryId,BasePrice,ImageUrl,IsActive")] Product product, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra nếu có file upload
                if (file != null && file.Length > 0)
                {
                    var result = await _photoService.AddPhotoAsync(file, "CafePos/Products"); // Thêm tên folder
                    product.ImageUrl = result.SecureUrl.ToString();
                }
                _context.Add(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm món mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            // Nếu lỗi, phải nạp lại danh sách Category cho Dropdown trước khi trả về View
            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(c => c.IsActive), "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // 4. DETAIL: Xem chi tiết 1 món
        public async Task<IActionResult> Detail(int? id) // Đổi tên thành Details cho giống View
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id && m.IsActive == true);

            if (product == null) return NotFound();

            return View(product);
        }

        // 5. EDIT (GET): Hiển thị form sửa
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // Nạp danh sách danh mục và chọn sẵn danh mục hiện tại của món đó
            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(c => c.IsActive), "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // 6. EDIT (POST): Xử lý cập nhật
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,Name,CategoryId,BasePrice,ImageUrl,IsActive")] Product product, IFormFile? file)
        {
            if (id != product.ProductId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Kiểm tra nếu có file upload từ máy tính
                    if (file != null && file.Length > 0)
                    {
                        var result = await _photoService.AddPhotoAsync(file, "CafePos/Products");
                        product.ImageUrl = result.SecureUrl.ToString(); // Lấy link từ Cloudinary
                    }
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật món thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.ProductId == product.ProductId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(c => c.IsActive), "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // 7. DELETE (POST): Xóa mềm (Ẩn món)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsActive = false;
                _context.Update(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã ẩn món khỏi thực đơn!";
            }
            return RedirectToAction(nameof(Index));
        }

        // 8. TRASH: Danh sách món đã ẩn
        public async Task<IActionResult> Trash()
        {
            var trashList = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive == false)
                .ToListAsync();
            return View(trashList);
        }

        // 9. RESTORE: Khôi phục món từ thùng rác
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Restore(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.IsActive = true;
            _context.Update(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Khôi phục món thành công!";
            return RedirectToAction(nameof(Trash));
        }
    }
}