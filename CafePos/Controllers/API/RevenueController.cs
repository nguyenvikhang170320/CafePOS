using CafePos.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafePos.Controllers.API
{
    [ApiController]
    [Route("api/revenue")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = "Employee"
    )]
    public class RevenueController : ControllerBase
    {
        private readonly CafePosDbContext _context;

        public RevenueController(CafePosDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET /api/revenue/today
        // DOANH THU HÔM NAY
        // =========================================================
        [HttpGet("today")]
        public async Task<IActionResult> GetTodayRevenue()
        {
            var now = DateTime.Now;

            var startDate = now.Date;
            var endDate = startDate.AddDays(1);

            // =====================================================
            // PAYMENT THÀNH CÔNG TRONG NGÀY
            // =====================================================
            var payments = await _context.Payments
                .AsNoTracking()
                .Where(x =>
                    x.IsSuccess &&
                    x.PaidAt >= startDate &&
                    x.PaidAt < endDate
                )
                .ToListAsync();

            // =====================================================
            // TỔNG DOANH THU
            // =====================================================
            var totalRevenue = payments.Sum(x => x.Amount);

            // =====================================================
            // TIỀN MẶT
            // =====================================================
            var cashRevenue = payments
                .Where(x => x.Method == "Cash")
                .Sum(x => x.Amount);

            // =====================================================
            // BANKING
            // =====================================================
            var bankingRevenue = payments
                .Where(x => x.Method == "Banking")
                .Sum(x => x.Amount);

            // =====================================================
            // ORDER ĐÃ THANH TOÁN TRONG NGÀY
            // =====================================================
            var orderIds = payments
                .Select(x => x.OrderId)
                .Distinct()
                .ToList();

            var completedOrders = await _context.Orders
                .AsNoTracking()
                .CountAsync(x =>
                    orderIds.Contains(x.OrderId) &&
                    x.OrderStatus == "Completed" &&
                    x.PaymentStatus == "Paid"
                );

            return Ok(new
            {
                success = true,

                data = new
                {
                    date = startDate,

                    totalRevenue,
                    cashRevenue,
                    bankingRevenue,

                    completedOrders,

                    totalPayments = payments.Count
                }
            });
        }
    }
}