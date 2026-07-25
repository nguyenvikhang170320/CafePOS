using CafePos.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafePos.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly CafePosDbContext _context;

        public HomeController(CafePosDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            // 1. Tổng doanh thu hôm nay
            var todayRevenue = await _context.Orders
                .Where(o => o.CreatedDate >= today && o.PaymentStatus != null && o.PaymentStatus.ToLower() == "paid")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // 2. Số đơn hàng hôm nay
            var todayOrderCount = await _context.Orders
                .CountAsync(o => o.CreatedDate >= today);

            // 3. Tổng số sản phẩm đang bán
            var productCount = await _context.Products
                .CountAsync(p => p.IsActive);

            // 4. Tổng số nhân viên
            var employeeCount = await _context.Employees
                .CountAsync(e => e.IsActive);

            // Truyền dữ liệu ra View qua ViewBag
            ViewBag.TodayRevenue = todayRevenue;
            ViewBag.TodayOrderCount = todayOrderCount;
            ViewBag.ProductCount = productCount;
            ViewBag.EmployeeCount = employeeCount;

            return View();
        }
    }
}