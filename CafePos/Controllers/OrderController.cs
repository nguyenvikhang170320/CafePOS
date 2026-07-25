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
    [Authorize(Roles = "Admin,Employee")]
    public class OrderController : Controller
    {
        private readonly CafePosDbContext _context;

        public OrderController(CafePosDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders.OrderByDescending(x => x.CreatedDate).ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> Invoice()
        {
            var orders = await _context.Orders.OrderByDescending(x => x.CreatedDate).ToListAsync();
            return View(orders);
        }

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

        [HttpGet]
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
                TempData["Message"] = "Đơn hàng này đã được thanh toán trước đó";
                TempData["MessageType"] = "warning";
                return RedirectToAction(nameof(Detail), new { id = order.OrderId });
            }

            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                TempData["Message"] = "Vui lòng chọn phương thức thanh toán";
                TempData["MessageType"] = "error";
                return View(order);
            }

            var firstItem = order.OrderItems.FirstOrDefault();
            if (firstItem != null)
            {
                if (firstItem.OrderItemToppings == null) firstItem.OrderItemToppings = new List<OrderItemTopping>();

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

            decimal subTotal = 0;
            foreach (var item in order.OrderItems)
            {
                decimal toppingTotal = item.OrderItemToppings?.Sum(x => x.TotalPrice) ?? 0;
                item.LineTotal = (item.UnitPrice * item.Quantity) + toppingTotal;
                subTotal += item.LineTotal;
            }

            order.SubTotal = subTotal;
            order.DiscountAmount = discountAmount < 0 ? 0 : discountAmount;
            order.TotalAmount = order.SubTotal - order.DiscountAmount;
            if (order.TotalAmount < 0) order.TotalAmount = 0;

            order.TableId = tableId;
            order.PaymentMethod = paymentMethod;
            order.PaymentStatus = "Paid";
            order.OrderStatus = "Completed";

            _context.Payments.Add(new Payment
            {
                OrderId = order.OrderId,
                Method = paymentMethod,
                Amount = order.TotalAmount,
                PaidAt = DateTime.Now
            });

            _context.OrderStatusLogs.Add(new OrderStatusLog
            {
                OrderId = order.OrderId,
                OldStatus = oldStatus,
                NewStatus = "Completed",
                ChangedAt = DateTime.Now,
                ChangedBy = User.Identity?.Name ?? "Thu ngân tại quầy"
            });

            await _context.SaveChangesAsync();

            TempData["Message"] = "Checkout thanh toán thành công";
            TempData["MessageType"] = "success";
            return RedirectToAction(nameof(Detail), new { id = order.OrderId });
        }
    }
}