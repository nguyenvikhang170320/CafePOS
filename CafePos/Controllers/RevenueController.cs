using CafePos.Data;
using CafePos.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CafePos.Controllers
{
    [Authorize(Roles = "Employee")]
    public class RevenueController : Controller
    {
        private readonly CafePosDbContext _context;

        public RevenueController(CafePosDbContext context)
        {
            _context = context;
        }

        // Action xem Báo cáo ca làm việc trong ngày của nhân viên
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var endOfDay = today.AddDays(1).AddTicks(-1);

            // 1. Lấy danh sách đơn hàng hoàn thành / đã thanh toán TRONG HÔM NAY
            var todayOrdersQuery = _context.Orders
                .Where(x => x.CreatedDate >= today && x.CreatedDate <= endOfDay
                         && (x.PaymentStatus == "Paid" || x.PaymentStatus == "Đã thanh toán"
                             || x.OrderStatus == "Completed" || x.OrderStatus == "Hoàn thành"));

            // 💡 LƯU Ý OPTIONAL: Nếu Model Order của bạn có cột UserId / EmployeeId, 
            // bỏ comment đoạn dưới để chỉ lấy đúng đơn do chính nhân viên đang đăng nhập tạo:
            /*
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(currentUserId))
            {
                todayOrdersQuery = todayOrdersQuery.Where(x => x.UserId == currentUserId);
            }
            */

            var todayOrdersList = await todayOrdersQuery
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            // 2. Tính toán tổng quan ca làm việc
            decimal totalRevenue = todayOrdersList.Sum(x => x.TotalAmount);
            int totalOrders = todayOrdersList.Count;

            // Phân loại doanh thu theo phương thức thanh toán (Tiền mặt / Chuyển khoản)
            // LƯU Ý: Tên chuỗi "Cash", "Bank", "Tiền mặt" cần khớp với DB của bạn
            decimal cashRevenue = todayOrdersList
                .Where(x => x.PaymentMethod == "Cash" || x.PaymentMethod == "Tiền mặt")
                .Sum(x => x.TotalAmount);

            decimal bankTransferRevenue = todayOrdersList
                .Where(x => x.PaymentMethod == "Bank" || x.PaymentMethod == "Chuyển khoản" || x.PaymentMethod == "Transfer" || x.PaymentMethod == "Momo" || x.PaymentMethod == "VNPay")
                .Sum(x => x.TotalAmount);

            // 3. Đưa dữ liệu vào ViewModel
            var model = new ShiftReportViewModel
            {
                ReportDate = today,
                EmployeeName = User.Identity?.Name ?? "Nhân viên quầy",
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                CashRevenue = cashRevenue,
                BankTransferRevenue = bankTransferRevenue,
                TodayOrders = todayOrdersList.Select(x => new OrderSummaryItem
                {
                    OrderId = x.OrderId, // Nếu thuộc tính ID trong Model Order của bạn tên là OrderId thì sửa x.Id thành x.OrderId
                    CreatedDate = x.CreatedDate,
                    TotalAmount = x.TotalAmount,
                    PaymentMethod = x.PaymentMethod ?? "Tiền mặt",
                    OrderStatus = x.OrderStatus
                }).ToList()
            };

            return View(model);
        }
    }
}
