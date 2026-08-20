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

        // =========================================================
        // BÁO CÁO CA LÀM VIỆC CỦA NHÂN VIÊN
        //
        // Payments:
        // - Ai thu tiền
        // - Thu bao nhiêu
        // - Phương thức gì
        // - Thời điểm nào
        //
        // Orders:
        // - Xác nhận đơn Paid + Completed
        // - Lấy thông tin đơn hàng
        // =========================================================
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // =====================================================
            // 1. USER ĐANG ĐĂNG NHẬP
            // =====================================================
            var userIdText = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (!int.TryParse(userIdText, out int currentUserId))
            {
                return Forbid();
            }

            // =====================================================
            // 2. EMPLOYEE ĐANG ĐĂNG NHẬP
            // =====================================================
            var employee = await _context.Employees
                .AsNoTracking()
                .Include(x => x.Position)
                .FirstOrDefaultAsync(x =>
                    x.UserId == currentUserId &&
                    x.IsActive
                );

            if (employee == null)
            {
                return NotFound(
                    "Không tìm thấy thông tin nhân viên."
                );
            }

            // =====================================================
            // 3. LẤY GIAO DỊCH CỦA NHÂN VIÊN
            //
            // JOIN Payments + Orders
            //
            // Chỉ tính:
            // - Payment thành công
            // - đúng EmployeeId
            // - PaidAt trong hôm nay
            // - Order Paid + Completed
            // =====================================================
            var transactions = await (
                from payment in _context.Payments.AsNoTracking()

                join order in _context.Orders.AsNoTracking()
                    on payment.OrderId equals order.OrderId

                where
                    payment.IsSuccess
                    &&
                    payment.EmployeeId == employee.EmployeeId
                    &&
                    payment.PaidAt >= today
                    &&
                    payment.PaidAt < tomorrow
                    &&
                    order.PaymentStatus == "Paid"
                    &&
                    order.OrderStatus == "Completed"

                orderby payment.PaidAt descending

                select new
                {
                    PaymentId = payment.PaymentId,

                    payment.OrderId,

                    payment.Method,

                    payment.Amount,

                    payment.PaidAt,

                    payment.EmployeeId,

                    payment.TransactionNo,

                    order.OrderCode,

                    order.CreatedDate,

                    order.TotalAmount,

                    order.PaymentStatus,

                    order.OrderStatus,

                    order.PaymentMethod
                }
            ).ToListAsync();

            // =====================================================
            // 4. TỔNG TIỀN NHÂN VIÊN THỰC TẾ ĐÃ THU
            // =====================================================
            decimal totalRevenue = transactions
                .Sum(x => x.Amount);

            // =====================================================
            // 5. TỔNG SỐ ĐƠN ĐÃ THU
            //
            // Distinct để tránh 1 đơn có nhiều Payment.
            // =====================================================
            int totalOrders = transactions
                .Select(x => x.OrderId)
                .Distinct()
                .Count();

            // =====================================================
            // 6. TIỀN MẶT
            // =====================================================
            decimal cashRevenue = transactions
                .Where(x =>
                    x.Method != null &&
                    x.Method.ToLower() == "cash"
                )
                .Sum(x => x.Amount);

            // =====================================================
            // 7. KHÔNG TIỀN MẶT
            //
            // Banking / Card / VNPAY / ...
            // =====================================================
            decimal bankTransferRevenue = transactions
                .Where(x =>
                    x.Method == null ||
                    x.Method.ToLower() != "cash"
                )
                .Sum(x => x.Amount);

            // =====================================================
            // 8. DANH SÁCH ORDER NHÂN VIÊN ĐÃ THU TIỀN
            //
            // Mỗi Order chỉ hiện 1 dòng.
            // =====================================================
            var todayOrders = transactions
                .GroupBy(x => x.OrderId)
                .Select(group =>
                {
                    var latest = group
                        .OrderByDescending(x => x.PaidAt)
                        .First();

                    var methods = group
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(x.Method)
                        )
                        .Select(x => x.Method!)
                        .Distinct()
                        .ToList();

                    return new OrderSummaryItem
                    {
                        OrderId =
                            latest.OrderId,

                        // Với báo cáo ca:
                        // dùng thời điểm nhận tiền gần nhất.
                        CreatedDate =
                            latest.PaidAt,

                        // Đây là số tiền NHÂN VIÊN NÀY đã thu
                        // cho đơn đó trong ngày.
                        TotalAmount =
                            group.Sum(x => x.Amount),

                        PaymentMethod =
                            methods.Count > 0
                                ? string.Join(", ", methods)
                                : "Chưa xác định",

                        OrderStatus =
                            latest.OrderStatus
                    };
                })
                .OrderByDescending(x => x.CreatedDate)
                .ToList();

            // =====================================================
            // 9. TÊN NHÂN VIÊN
            // =====================================================
            var employeeName =
                employee.Position != null
                    ? $"{employee.FullName} ({employee.Position.PositionName})"
                    : employee.FullName;

            // =====================================================
            // 10. VIEW MODEL
            // =====================================================
            var model = new ShiftReportViewModel
            {
                ReportDate =
                    today,

                EmployeeName =
                    employeeName,

                TotalRevenue =
                    totalRevenue,

                TotalOrders =
                    totalOrders,

                CashRevenue =
                    cashRevenue,

                BankTransferRevenue =
                    bankTransferRevenue,

                TodayOrders =
                    todayOrders
            };

            // =====================================================
            // THÔNG TIN ĐỐI SOÁT BỔ SUNG
            // Không bắt buộc View phải sử dụng.
            // =====================================================
            ViewBag.EmployeeId =
                employee.EmployeeId;

            ViewBag.PositionName =
                employee.Position?.PositionName;

            ViewBag.TransactionCount =
                transactions.Count;

            return View(model);
        }
    }
}