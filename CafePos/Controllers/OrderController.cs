using CafePos.Data;
using CafePos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CafePos.Controllers
{
    // 🔒 Phân quyền dành cho Nhân viên và Admin
    [Authorize(Roles = "Admin,Employee")]
    public class OrderController : Controller
    {
        private readonly CafePosDbContext _context;

        public OrderController(CafePosDbContext context)
        {
            _context = context;
        }

        // 1. Danh sách TẤT CẢ đơn hàng (Dành cho Nhân viên quản lý / Thu ngân)
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(x => x.Table)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return View(orders);
        }

        // 2. Danh sách TẤT CẢ hóa đơn + Khung xem AJAX
        public async Task<IActionResult> Invoice()
        {
            var orders = await _context.Orders
                .Include(x => x.Table)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return View(orders);
        }

        // 3. Chi tiết hóa đơn full page
        public async Task<IActionResult> Detail(int id)
        {
            var order = await _context.Orders
                .Include(x => x.Table)
                .Include(x => x.OrderItems)
                    .ThenInclude(x => x.OrderItemToppings)
                .FirstOrDefaultAsync(x => x.OrderId == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // 4. GET: Checkout (Màn hình tính tiền tại quầy)
        public async Task<IActionResult> Checkout(int id)
        {
            var order = await _context.Orders
                .Include(x => x.Table)
                .Include(x => x.OrderItems)
                    .ThenInclude(x => x.OrderItemToppings)
                .FirstOrDefaultAsync(x => x.OrderId == id);

            if (order == null) return NotFound();

            ViewBag.TablesList = await _context.Tables.Where(t => t.IsActive).ToListAsync();
            ViewBag.Toppings = await _context.Toppings.Where(x => x.IsActive).ToListAsync();

            return View(order);
        }

        // 5. POST: Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int id, List<int>? selectedToppingIds, decimal discountAmount, string paymentMethod, int? tableId)
        {
            ViewBag.TablesList = await _context.Tables.Where(t => t.IsActive).ToListAsync();
            ViewBag.Toppings = await _context.Toppings.Where(x => x.IsActive).ToListAsync();

            var order = await _context.Orders
                .Include(x => x.OrderItems)
                    .ThenInclude(x => x.OrderItemToppings)
                .FirstOrDefaultAsync(x => x.OrderId == id);

            if (order == null) return NotFound();

            string oldStatus = order.OrderStatus;

            if (order.PaymentStatus == "Paid" || order.OrderStatus == "Completed")
            {
                TempData["Message"] = "Đơn hàng này đã được thanh toán rồi";
                TempData["MessageType"] = "warning";
                return RedirectToAction("Detail", new { id = order.OrderId });
            }

            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                TempData["Message"] = "Vui lòng chọn phương thức thanh toán";
                TempData["MessageType"] = "error";
                return View(order);
            }

            var firstItem = order.OrderItems?.FirstOrDefault();
            if (firstItem != null)
            {
                if (firstItem.OrderItemToppings == null)
                    firstItem.OrderItemToppings = new List<OrderItemTopping>();

                // Thêm topping nếu nhân viên chọn thêm tại quầy
                if (selectedToppingIds != null && selectedToppingIds.Any())
                {
                    var toppings = await _context.Toppings
                        .Where(x => selectedToppingIds.Contains(x.ToppingId) && x.IsActive)
                        .ToListAsync();

                    foreach (var topping in toppings)
                    {
                        bool existed = firstItem.OrderItemToppings.Any(x => x.ToppingId == topping.ToppingId);
                        if (!existed)
                        {
                            firstItem.OrderItemToppings.Add(new OrderItemTopping
                            {
                                ToppingId = topping.ToppingId,
                                ToppingNameSnapshot = topping.Name,
                                Price = topping.Price,
                                Quantity = 1,
                                TotalPrice = topping.Price
                            });
                        }
                    }
                }
            }

            // Tính toán lại tổng tiền đơn hàng
            decimal subTotal = 0;
            if (order.OrderItems != null)
            {
                foreach (var item in order.OrderItems)
                {
                    decimal toppingTotal = item.OrderItemToppings?.Sum(x => x.TotalPrice) ?? 0;
                    item.LineTotal = (item.UnitPrice * item.Quantity) + toppingTotal;
                    subTotal += item.LineTotal;
                }
            }

            order.SubTotal = subTotal;
            order.DiscountAmount = discountAmount < 0 ? 0 : discountAmount;
            order.TotalAmount = order.SubTotal - order.DiscountAmount;
            if (order.TotalAmount < 0) order.TotalAmount = 0;

            order.TableId = tableId;
            order.PaymentMethod = paymentMethod;
            order.PaymentStatus = "Paid";
            order.OrderStatus = "Completed";

            // 1. Lưu giao dịch thanh toán vào bảng Payments
            _context.Payments.Add(new Payment
            {
                OrderId = order.OrderId,
                Method = paymentMethod,
                Amount = order.TotalAmount,
                PaidAt = DateTime.Now
            });

            // 2. Ghi nhật ký đổi trạng thái vào OrderStatusLogs
            _context.OrderStatusLogs.Add(new OrderStatusLog
            {
                OrderId = order.OrderId,
                OldStatus = oldStatus,
                NewStatus = "Completed",
                ChangedAt = DateTime.Now,
                ChangedBy = User.Identity?.Name ?? "Thu ngân"
            });

            await _context.SaveChangesAsync();

            TempData["Message"] = "Checkout thành công!";
            TempData["MessageType"] = "success";

            return RedirectToAction("Detail", new { id = order.OrderId });
        }

        // 6. AJAX: Trả partial view chi tiết hóa đơn
        [HttpGet]
        public async Task<IActionResult> GetInvoicePartial(int id)
        {
            var order = await _context.Orders
                .Include(x => x.Table)
                .Include(x => x.OrderItems)
                    .ThenInclude(x => x.OrderItemToppings)
                .FirstOrDefaultAsync(x => x.OrderId == id);

            if (order == null) return NotFound();

            return PartialView("_InvoicePartial", order);
        }

        // 7. AJAX: Đánh dấu đã thanh toán (Admin / Thu ngân)
        [HttpPost]
        public async Task<IActionResult> MarkAsPaid(int id, string paymentMethod)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == id);

            if (order == null)
                return Json(new { success = false, message = "Không tìm thấy hóa đơn" });

            order.PaymentStatus = "Paid";
            order.PaymentMethod = paymentMethod;
            order.OrderStatus = "Completed";

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Thanh toán thành công" });
        }

        // 8. In hóa đơn
        public async Task<IActionResult> PrintInvoice(int id)
        {
            var order = await _context.Orders
                .Include(x => x.Table)
                .Include(x => x.OrderItems)
                    .ThenInclude(x => x.OrderItemToppings)
                .FirstOrDefaultAsync(x => x.OrderId == id);

            if (order == null) return NotFound();

            return View(order);
        }
    }
}