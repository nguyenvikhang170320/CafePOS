using CafePos.Data;
using CafePos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CafePos.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
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

        // =========================
        // THÊM MỚI
        // =========================
        public IActionResult Create()
        {
            LoadProductDropDownList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductSize productSize)
        {
            if (!ModelState.IsValid)
            {
                LoadProductDropDownList(productSize.ProductId);
                return View(productSize);
            }

            string sizeName = productSize.SizeName?.Trim().ToLower() ?? "";

            bool isDuplicate = await _context.ProductSizes
                .IgnoreQueryFilters()
                .AnyAsync(x =>
                    !x.IsDeleted &&
                    x.ProductId == productSize.ProductId &&
                    x.SizeName.ToLower() == sizeName);

            if (isDuplicate)
            {
                ModelState.AddModelError("SizeName", "Size này đã tồn tại cho sản phẩm đã chọn.");
                LoadProductDropDownList(productSize.ProductId);
                return View(productSize);
            }

            productSize.SizeName = productSize.SizeName?.Trim();
            productSize.IsDeleted = false;

            _context.ProductSizes.Add(productSize);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm size sản phẩm thành công.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // CHỈNH SỬA
        // =========================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var productSize = await _context.ProductSizes
                .FirstOrDefaultAsync(x => x.ProductSizeId == id && !x.IsDeleted);

            if (productSize == null) return NotFound();

            LoadProductDropDownList(productSize.ProductId);
            return View(productSize);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductSize productSize)
        {
            if (id != productSize.ProductSizeId) return NotFound();

            if (!ModelState.IsValid)
            {
                LoadProductDropDownList(productSize.ProductId);
                return View(productSize);
            }

            var existing = await _context.ProductSizes
                .FirstOrDefaultAsync(x => x.ProductSizeId == id && !x.IsDeleted);

            if (existing == null) return NotFound();

            string sizeName = productSize.SizeName?.Trim().ToLower() ?? "";

            bool isDuplicate = await _context.ProductSizes
                .IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.ProductSizeId != productSize.ProductSizeId &&
                    !x.IsDeleted &&
                    x.ProductId == productSize.ProductId &&
                    x.SizeName.ToLower() == sizeName);

            if (isDuplicate)
            {
                ModelState.AddModelError("SizeName", "Size này đã tồn tại cho sản phẩm đã chọn.");
                LoadProductDropDownList(productSize.ProductId);
                return View(productSize);
            }

            try
            {
                existing.ProductId = productSize.ProductId;
                existing.SizeName = productSize.SizeName?.Trim();
                existing.ExtraPrice = productSize.ExtraPrice;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Cập nhật size sản phẩm thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ProductSizeExists(productSize.ProductSizeId))
                    return NotFound();

                throw;
            }
        }

        // =========================
        // XÓA MỀM
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var productSize = await _context.ProductSizes
                .FirstOrDefaultAsync(x => x.ProductSizeId == id && !x.IsDeleted);

            if (productSize == null) return NotFound();

            productSize.IsDeleted = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã chuyển size sản phẩm vào thùng rác.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // THÙNG RÁC
        // =========================
        public async Task<IActionResult> Trash()
        {
            var deletedProductSizes = await _context.ProductSizes
                .IgnoreQueryFilters()
                .Where(x => x.IsDeleted)
                .Include(x => x.Product)
                .OrderByDescending(x => x.ProductSizeId)
                .ToListAsync();

            return View(deletedProductSizes);
        }

        // =========================
        // KHÔI PHỤC
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var productSize = await _context.ProductSizes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.ProductSizeId == id);

            if (productSize == null) return NotFound();

            string sizeName = productSize.SizeName?.Trim().ToLower() ?? "";

            bool isDuplicate = await _context.ProductSizes
                .IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.ProductSizeId != productSize.ProductSizeId &&
                    !x.IsDeleted &&
                    x.ProductId == productSize.ProductId &&
                    x.SizeName.ToLower() == sizeName);

            if (isDuplicate)
            {
                TempData["Error"] = "Không thể khôi phục vì size này đã tồn tại lại trong danh sách đang hoạt động.";
                return RedirectToAction(nameof(Trash));
            }

            productSize.IsDeleted = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Khôi phục size sản phẩm thành công.";
            return RedirectToAction(nameof(Trash));
        }

        // =========================
        // XÓA CỨNG
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HardDelete(int id)
        {
            var productSize = await _context.ProductSizes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.ProductSizeId == id);

            if (productSize == null) return NotFound();

            bool isUsedInOrder = await _context.OrderItems
                .AnyAsync(x => x.ProductSizeId == id);

            if (isUsedInOrder)
            {
                TempData["Error"] = "Không thể xóa vĩnh viễn vì size này đã được sử dụng trong hóa đơn.";
                return RedirectToAction(nameof(Trash));
            }

            try
            {
                _context.ProductSizes.Remove(productSize);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã xóa vĩnh viễn size sản phẩm.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Không thể xóa vĩnh viễn size sản phẩm do đang có dữ liệu liên quan.";
            }

            return RedirectToAction(nameof(Trash));
        }

        // =========================
        // HÀM PHỤ
        // =========================
        private void LoadProductDropDownList(object? selectedProduct = null)
        {
            ViewBag.ProductId = new SelectList(
                _context.Products
                    .OrderBy(p => p.Name)
                    .ToList(),
                "ProductId",
                "Name",
                selectedProduct
            );
        }

        private async Task<bool> ProductSizeExists(int id)
        {
            return await _context.ProductSizes
                .IgnoreQueryFilters()
                .AnyAsync(e => e.ProductSizeId == id);
        }
    }
}