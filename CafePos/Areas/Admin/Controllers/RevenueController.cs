using CafePos.Data;
using CafePos.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafePos.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RevenueController : Controller
    {
        private readonly CafePosDbContext _context;

        public RevenueController(CafePosDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // DASHBOARD DOANH THU ADMIN
        //
        // ORDERS:
        // - Doanh thu chính thức của quán
        // - Doanh thu tháng / năm / 7 ngày
        // - Số đơn
        // - Top món bán chạy
        //
        // PAYMENTS:
        // - Giao dịch thực thu
        // - Phương thức thanh toán
        // - Nhân viên thực hiện thu tiền
        // - Đối soát với Orders
        // =========================================================
        public async Task<IActionResult> Index(int? year)
        {
            int selectedYear =
                year ?? DateTime.Now.Year;

            var startOfYear =
                new DateTime(selectedYear, 1, 1);

            var startOfNextYear =
                startOfYear.AddYears(1);


            // =====================================================
            // 1. ORDERS HỢP LỆ
            //
            // Chỉ Paid + Completed mới tính doanh thu.
            // =====================================================
            var validOrders = _context.Orders
                .AsNoTracking()
                .Where(x =>
                    x.PaymentStatus == "Paid" &&
                    x.OrderStatus == "Completed"
                );


            // =====================================================
            // 2. DOANH THU THEO THÁNG
            // =====================================================
            var monthlyRevenue = await validOrders
                .Where(x =>
                    x.CreatedDate >= startOfYear &&
                    x.CreatedDate < startOfNextYear
                )
                .GroupBy(x => new
                {
                    x.CreatedDate.Year,
                    x.CreatedDate.Month
                })
                .Select(g => new RevenueByMonthViewModel
                {
                    Year =
                        g.Key.Year,

                    Month =
                        g.Key.Month,

                    TotalRevenue =
                        g.Sum(x => x.TotalAmount),

                    TotalOrders =
                        g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();


            // =====================================================
            // 3. DOANH THU THEO NĂM
            // =====================================================
            var yearlyRevenue = await validOrders
                .GroupBy(x =>
                    x.CreatedDate.Year
                )
                .Select(g => new RevenueByYearViewModel
                {
                    Year =
                        g.Key,

                    TotalRevenue =
                        g.Sum(x => x.TotalAmount),

                    TotalOrders =
                        g.Count()
                })
                .OrderBy(x => x.Year)
                .ToListAsync();


            // =====================================================
            // 4. TOP 10 MÓN BÁN CHẠY
            //
            // Chỉ lấy món thuộc đơn Paid + Completed.
            // =====================================================
            var bestSellingProducts = await _context.OrderItems
                .AsNoTracking()
                .Include(x => x.Order)
                .Where(x =>
                    x.Order != null &&
                    x.Order.PaymentStatus == "Paid" &&
                    x.Order.OrderStatus == "Completed"
                )
                .GroupBy(x => new
                {
                    x.ProductId,
                    x.ProductNameSnapshot
                })
                .Select(g => new BestSellingProductViewModel
                {
                    ProductId =
                        g.Key.ProductId,

                    ProductName =
                        g.Key.ProductNameSnapshot,

                    TotalQuantitySold =
                        g.Sum(x => x.Quantity),

                    TotalRevenue =
                        g.Sum(x => x.LineTotal)
                })
                .OrderByDescending(x =>
                    x.TotalQuantitySold
                )
                .Take(10)
                .ToListAsync();


            // =====================================================
            // 5. DOANH THU 7 NGÀY GẦN NHẤT
            // =====================================================
            var startDate =
                DateTime.Today.AddDays(-6);

            var tomorrow =
                DateTime.Today.AddDays(1);

            var raw7DaysData = await validOrders
                .Where(x =>
                    x.CreatedDate >= startDate &&
                    x.CreatedDate < tomorrow
                )
                .GroupBy(x =>
                    x.CreatedDate.Date
                )
                .Select(g => new
                {
                    Date =
                        g.Key,

                    TotalRevenue =
                        g.Sum(x => x.TotalAmount),

                    TotalOrders =
                        g.Count()
                })
                .ToListAsync();


            // =====================================================
            // 6. LẤP ĐẦY ĐỦ 7 NGÀY
            // =====================================================
            var last7DaysRevenue = Enumerable
                .Range(0, 7)
                .Select(offset =>
                    startDate.AddDays(offset)
                )
                .Select(date =>
                {
                    var dayData =
                        raw7DaysData.FirstOrDefault(
                            x => x.Date == date
                        );

                    return new DailyRevenueViewModel
                    {
                        Date =
                            date,

                        TotalRevenue =
                            dayData?.TotalRevenue ?? 0,

                        TotalOrders =
                            dayData?.TotalOrders ?? 0
                    };
                })
                .ToList();


            // =====================================================
            // 7. DOANH THU CHÍNH THỨC
            // ORDERS
            // =====================================================
            decimal officialRevenue =
                monthlyRevenue.Sum(
                    x => x.TotalRevenue
                );

            int officialOrderCount =
                monthlyRevenue.Sum(
                    x => x.TotalOrders
                );


            // =====================================================
            // 8. PAYMENTS TRONG NĂM
            //
            // JOIN:
            // Payments
            // Orders
            // Employees
            // Positions
            //
            // Chỉ tính Payment thành công và Order Paid + Completed.
            // =====================================================
            var paymentRows = await (
                from payment in _context.Payments.AsNoTracking()

                join order in _context.Orders.AsNoTracking()
                    on payment.OrderId equals order.OrderId

                join employee in _context.Employees.AsNoTracking()
                    on payment.EmployeeId equals employee.EmployeeId

                join position in _context.Positions.AsNoTracking()
                    on employee.PositionId equals position.PositionId
                    into positionJoin

                from position in positionJoin.DefaultIfEmpty()

                where
                    payment.IsSuccess
                    &&
                    payment.PaidAt >= startOfYear
                    &&
                    payment.PaidAt < startOfNextYear
                    &&
                    order.PaymentStatus == "Paid"
                    &&
                    order.OrderStatus == "Completed"

                select new
                {
                    payment.PaymentId,

                    payment.OrderId,

                    payment.Method,

                    payment.Amount,

                    payment.PaidAt,

                    payment.EmployeeId,

                    EmployeeName =
                        employee.FullName,

                    PositionName =
                        position != null
                            ? position.PositionName
                            : "Chưa xác định"
                }
            ).ToListAsync();


            // =====================================================
            // 9. TỔNG PAYMENT ĐÃ GHI NHẬN
            // =====================================================
            decimal recordedPaymentRevenue =
                paymentRows.Sum(
                    x => x.Amount
                );


            // =====================================================
            // 10. PAYMENT THEO PHƯƠNG THỨC
            // =====================================================
            decimal cashRevenue = paymentRows
                .Where(x =>
                    string.Equals(
                        x.Method,
                        "Cash",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .Sum(x => x.Amount);


            decimal bankingRevenue = paymentRows
                .Where(x =>
                    string.Equals(
                        x.Method,
                        "Banking",
                        StringComparison.OrdinalIgnoreCase
                    )
                    ||
                    string.Equals(
                        x.Method,
                        "Bank",
                        StringComparison.OrdinalIgnoreCase
                    )
                    ||
                    string.Equals(
                        x.Method,
                        "Transfer",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .Sum(x => x.Amount);


            decimal cardRevenue = paymentRows
                .Where(x =>
                    string.Equals(
                        x.Method,
                        "Card",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .Sum(x => x.Amount);


            decimal vnPayRevenue = paymentRows
                .Where(x =>
                    string.Equals(
                        x.Method,
                        "VNPAY",
                        StringComparison.OrdinalIgnoreCase
                    )
                    ||
                    string.Equals(
                        x.Method,
                        "VNPay",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .Sum(x => x.Amount);


            // =====================================================
            // 11. ĐỐI SOÁT
            //
            // Không cộng Orders + Payments.
            //
            // Orders = doanh thu chính thức
            // Payments = giao dịch đã ghi nhận
            // =====================================================
            decimal reconciliationDifference =
                officialRevenue -
                recordedPaymentRevenue;


            // =====================================================
            // 12. DOANH THU THEO NHÂN VIÊN
            //
            // Group Payments theo EmployeeId.
            //
            // Đây là số tiền nhân viên trực tiếp đã thu.
            // =====================================================
            var employeeRevenues = paymentRows
                .GroupBy(x => new
                {
                    x.EmployeeId,
                    x.EmployeeName,
                    x.PositionName
                })
                .Select(g =>
                {
                    decimal employeeCash = g
                        .Where(x =>
                            string.Equals(
                                x.Method,
                                "Cash",
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        .Sum(x => x.Amount);


                    decimal employeeBanking = g
                        .Where(x =>
                            string.Equals(
                                x.Method,
                                "Banking",
                                StringComparison.OrdinalIgnoreCase
                            )
                            ||
                            string.Equals(
                                x.Method,
                                "Bank",
                                StringComparison.OrdinalIgnoreCase
                            )
                            ||
                            string.Equals(
                                x.Method,
                                "Transfer",
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        .Sum(x => x.Amount);


                    decimal employeeCard = g
                        .Where(x =>
                            string.Equals(
                                x.Method,
                                "Card",
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        .Sum(x => x.Amount);


                    decimal employeeVnPay = g
                        .Where(x =>
                            string.Equals(
                                x.Method,
                                "VNPAY",
                                StringComparison.OrdinalIgnoreCase
                            )
                            ||
                            string.Equals(
                                x.Method,
                                "VNPay",
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        .Sum(x => x.Amount);


                    decimal knownRevenue =
                        employeeCash +
                        employeeBanking +
                        employeeCard +
                        employeeVnPay;


                    decimal totalEmployeeRevenue =
                        g.Sum(x => x.Amount);


                    return new EmployeeRevenueViewModel
                    {
                        EmployeeId =
                            g.Key.EmployeeId,

                        EmployeeName =
                            g.Key.EmployeeName,

                        PositionName =
                            g.Key.PositionName,

                        TotalTransactions =
                            g.Count(),

                        TotalOrders =
                            g.Select(x => x.OrderId)
                                .Distinct()
                                .Count(),

                        CashRevenue =
                            employeeCash,

                        BankingRevenue =
                            employeeBanking,

                        CardRevenue =
                            employeeCard,

                        VnPayRevenue =
                            employeeVnPay,

                        OtherRevenue =
                            totalEmployeeRevenue -
                            knownRevenue,

                        TotalRevenue =
                            totalEmployeeRevenue
                    };
                })
                .OrderByDescending(x =>
                    x.TotalRevenue
                )
                .ThenBy(x =>
                    x.EmployeeName
                )
                .ToList();


            // =====================================================
            // 13. VIEWBAG ĐỐI SOÁT
            // =====================================================
            ViewBag.OfficialRevenue =
                officialRevenue;

            ViewBag.RecordedPaymentRevenue =
                recordedPaymentRevenue;

            ViewBag.ReconciliationDifference =
                reconciliationDifference;

            ViewBag.CashRevenue =
                cashRevenue;

            ViewBag.BankingRevenue =
                bankingRevenue;

            ViewBag.CardRevenue =
                cardRevenue;

            ViewBag.VnPayRevenue =
                vnPayRevenue;

            ViewBag.PaymentTransactionCount =
                paymentRows.Count;


            // =====================================================
            // 14. DASHBOARD
            // =====================================================
            var dashboard =
                new ReportDashboardViewModel
                {
                    SelectedYear =
                        selectedYear,

                    TotalRevenueThisYear =
                        officialRevenue,

                    TotalOrdersThisYear =
                        officialOrderCount,

                    MonthlyRevenues =
                        monthlyRevenue,

                    YearlyRevenues =
                        yearlyRevenue,

                    BestSellingProducts =
                        bestSellingProducts,

                    Last7DaysRevenues =
                        last7DaysRevenue,

                    EmployeeRevenues =
                        employeeRevenues
                };


            return View(dashboard);
        }
    }
}