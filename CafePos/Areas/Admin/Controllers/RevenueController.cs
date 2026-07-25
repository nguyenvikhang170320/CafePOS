using CafePos.Data;
using CafePos.Models;
using CafePos.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafePos.Areas.Admin.Controllers
{
    // 💡 LƯU Ý: Nếu Controller này nằm trong thư mục Areas/Admin, nhớ thêm [Area("Admin")] vào đây nhé!
    [Area("Admin")]
    public class RevenueController : Controller
    {
        private readonly CafePosDbContext _context;

        public RevenueController(CafePosDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? year)
        {
            int selectedYear = year ?? DateTime.Now.Year;

            // 1. Lọc đơn hàng hợp lệ trong năm được chọn
            var paidOrders = _context.Orders
                .Where(x => x.CreatedDate.Year == selectedYear
                         && (x.PaymentStatus == "Paid" || x.PaymentStatus == "Đã thanh toán"
                             || x.OrderStatus == "Completed" || x.OrderStatus == "Hoàn thành"));

            // 2. Thống kê theo Tháng
            var monthlyRevenue = await paidOrders
                .GroupBy(x => new { x.CreatedDate.Year, x.CreatedDate.Month })
                .Select(g => new RevenueByMonthViewModel
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalRevenue = g.Sum(x => x.TotalAmount),
                    TotalOrders = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // 3. Thống kê theo Năm
            var yearlyRevenue = await _context.Orders
                .Where(x => x.PaymentStatus == "Paid" || x.PaymentStatus == "Đã thanh toán"
                         || x.OrderStatus == "Completed" || x.OrderStatus == "Hoàn thành")
                .GroupBy(x => x.CreatedDate.Year)
                .Select(g => new RevenueByYearViewModel
                {
                    Year = g.Key,
                    TotalRevenue = g.Sum(x => x.TotalAmount),
                    TotalOrders = g.Count()
                })
                .OrderBy(x => x.Year)
                .ToListAsync();

            // 4. Top 10 Món bán chạy
            var bestSellingProducts = await _context.OrderItems
                .Include(x => x.Order)
                .Where(x => x.Order != null &&
                           (x.Order.PaymentStatus == "Paid" || x.Order.PaymentStatus == "Đã thanh toán"
                         || x.Order.OrderStatus == "Completed" || x.Order.OrderStatus == "Hoàn thành"))
                .GroupBy(x => new { x.ProductId, x.ProductNameSnapshot })
                .Select(g => new BestSellingProductViewModel
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductNameSnapshot,
                    TotalQuantitySold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.LineTotal)
                })
                .OrderByDescending(x => x.TotalQuantitySold)
                .Take(10)
                .ToListAsync();

            // 🌟 5. BỔ SUNG MỚI: Thống kê Doanh thu 7 ngày gần nhất cho Chart.js
            var startDate = DateTime.Today.AddDays(-6); // Lấy 6 ngày trước + hôm nay = 7 ngày
            var endDate = DateTime.Today.AddDays(1).AddTicks(-1);

            var raw7DaysData = await _context.Orders
                .Where(x => x.CreatedDate >= startDate && x.CreatedDate <= endDate
                         && (x.PaymentStatus == "Paid" || x.PaymentStatus == "Đã thanh toán"
                             || x.OrderStatus == "Completed" || x.OrderStatus == "Hoàn thành"))
                .GroupBy(x => x.CreatedDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalRevenue = g.Sum(x => x.TotalAmount),
                    TotalOrders = g.Count()
                })
                .ToListAsync();

            // Lấp đầy các ngày không có doanh thu (đảm bảo đủ 7 ngày liên tục trên biểu đồ)
            var last7DaysRevenue = Enumerable.Range(0, 7)
                .Select(offset => startDate.AddDays(offset))
                .Select(date =>
                {
                    var dayData = raw7DaysData.FirstOrDefault(x => x.Date == date);
                    return new DailyRevenueViewModel
                    {
                        Date = date,
                        TotalRevenue = dayData?.TotalRevenue ?? 0,
                        TotalOrders = dayData?.TotalOrders ?? 0
                    };
                })
                .ToList();

            // 6. Đưa tất cả dữ liệu vào ReportDashboardViewModel
            var dashboard = new ReportDashboardViewModel
            {
                SelectedYear = selectedYear,
                TotalRevenueThisYear = monthlyRevenue.Sum(x => x.TotalRevenue),
                TotalOrdersThisYear = monthlyRevenue.Sum(x => x.TotalOrders),
                MonthlyRevenues = monthlyRevenue,
                YearlyRevenues = yearlyRevenue,
                BestSellingProducts = bestSellingProducts,
                Last7DaysRevenues = last7DaysRevenue // 👈 Đã gán danh sách 7 ngày vào đây
            };

            return View(dashboard);
        }
    }
}
