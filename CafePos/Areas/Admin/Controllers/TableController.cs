using System.Threading.Tasks;
using CafePos.Models;
using CafePos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafePos.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class TableController : Controller
    {
        private readonly ITableService _tableService;

        public TableController(ITableService tableService)
        {
            _tableService = tableService;
        }

        public async Task<IActionResult> Index()
        {
            var tables = await _tableService.GetAllTablesAsync();
            return View(tables);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Table table)
        {
            if (ModelState.IsValid)
            {
                await _tableService.CreateTableAsync(table);
                TempData["SuccessMessage"] = "Thêm bàn mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(table);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var table = await _tableService.GetTableByIdAsync(id);
            if (table == null) return NotFound();
            return View(table);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Table table)
        {
            if (id != table.TableId) return NotFound();

            if (ModelState.IsValid)
            {
                await _tableService.UpdateTableAsync(table);
                TempData["SuccessMessage"] = "Cập nhật thông tin bàn thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(table);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            await _tableService.ToggleTableStatusAsync(id);
            TempData["SuccessMessage"] = "Đã thay đổi trạng thái hoạt động của bàn!";
            return RedirectToAction(nameof(Index));
        }
    }
}