using CafePos.Data;
using CafePos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CafePos.Controllers
{
    [Authorize(Roles = "Staff")] // 🔒 Bắt buộc đăng nhập
    public class CustomerOrderController : Controller
    {
        private readonly CafePosDbContext _context;

        public CustomerOrderController(CafePosDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCreateViewData();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string? note,
            string? phone,
            string? paymentMethod, // 🌟 Nhận phương thức thanh toán
            List<int> productIds,
            List<int> quantities,
            List<int?>? productSizeIds)
        {
            await LoadCreateViewData();

            var customerName = User.FindFirst("FullName")?.Value
                               ?? User.Identity?.Name
                               ?? "Khách hàng";

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(phone))
            {
                TempData["Message"] = "Vui lòng nhập số điện thoại";
                TempData["MessageType"] = "error";
                return View();
            }

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                TempData["Message"] = "Không xác định được tài khoản đăng nhập";
                TempData["MessageType"] = "error";
                return View();
            }

            if (productIds == null || quantities == null || !productIds.Any() || !quantities.Any())
            {
                TempData["Message"] = "Dữ liệu món chọn không hợp lệ";
                TempData["MessageType"] = "error";
                return View();
            }

            // Lấy toàn bộ Toppings trong DB để tra cứu
            var allToppings = await _context.Toppings.Where(x => x.IsActive).ToListAsync();

            var validItems = new List<OrderItem>();
            decimal subTotal = 0;

            for (int i = 0; i < productIds.Count; i++)
            {
                if (i >= quantities.Count) continue;

                int quantity = quantities[i];
                if (quantity <= 0) continue; // Chỉ xử lý món có chọn số lượng > 0

                var product = await _context.Products
                    .FirstOrDefaultAsync(x => x.ProductId == productIds[i] && x.IsActive);

                if (product == null) continue;

                decimal extraPrice = 0;
                string? sizeName = null;
                int? selectedSizeId = null;

                if (productSizeIds != null && i < productSizeIds.Count)
                {
                    selectedSizeId = productSizeIds[i];
                    if (selectedSizeId.HasValue && selectedSizeId.Value > 0)
                    {
                        var productSize = await _context.ProductSizes
                            .FirstOrDefaultAsync(ps => ps.ProductSizeId == selectedSizeId.Value && ps.ProductId == product.ProductId);

                        if (productSize != null)
                        {
                            extraPrice = productSize.ExtraPrice;
                            sizeName = productSize.SizeName;
                        }
                    }
                }

                var orderItem = new OrderItem
                {
                    ProductId = product.ProductId,
                    Quantity = quantity,
                    ProductNameSnapshot = product.Name,
                    UnitPrice = product.BasePrice + extraPrice,
                    ProductSizeId = selectedSizeId,
                    SizeNameSnapshot = sizeName,
                    ItemNote = null,
                    OrderItemToppings = new List<OrderItemTopping>()
                };

                // 🌟 BẮT CHÍNH XÁC TOPPING TỪ FORM DỰA VÀO CHỈ MỤC i (Tương ứng với itemToppings_0, itemToppings_1...)
                var toppingFormValues = Request.Form["itemToppings_" + i];
                decimal toppingTotal = 0;

                if (!string.IsNullOrEmpty(toppingFormValues))
                {
                    // Lấy toàn bộ ID topping được tick của món thứ i
                    var selectedToppingIds = toppingFormValues.ToString()
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(int.Parse)
                        .ToList();

                    foreach (var topId in selectedToppingIds)
                    {
                        var top = allToppings.FirstOrDefault(t => t.ToppingId == topId);
                        if (top != null)
                        {
                            var itemTopping = new OrderItemTopping
                            {
                                ToppingId = top.ToppingId,
                                ToppingNameSnapshot = top.Name,
                                Price = top.Price,
                                Quantity = quantity,
                                TotalPrice = top.Price * quantity
                            };

                            orderItem.OrderItemToppings.Add(itemTopping);
                            toppingTotal += itemTopping.TotalPrice;
                        }
                    }
                }

                orderItem.LineTotal = (orderItem.UnitPrice * quantity) + toppingTotal;
                subTotal += orderItem.LineTotal;

                validItems.Add(orderItem);
            }

            if (!validItems.Any())
            {
                TempData["Message"] = "Vui lòng chọn ít nhất 1 món và nhập số lượng lớn hơn 0";
                TempData["MessageType"] = "error";
                return View();
            }

            var order = new Order
            {
                OrderCode = "ORD" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                CustomerName = customerName,
                CustomerPhone = phone,
                UserId = userId,
                CreatedDate = DateTime.Now,
                Note = note,
                OrderStatus = "Pending",
                PaymentStatus = "Unpaid",
                PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "Cash" : paymentMethod,
                SubTotal = subTotal,
                DiscountAmount = 0,
                TotalAmount = subTotal,
                OrderItems = validItems
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Đặt món thành công";
            TempData["MessageType"] = "success";

            return RedirectToAction(nameof(MyOrders));
        }

        private async Task LoadCreateViewData()
        {
            ViewBag.Products = await _context.Products
                .Include(p => p.ProductSizes.Where(ps => !ps.IsDeleted))
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            ViewBag.Toppings = await _context.Toppings
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            ViewBag.CustomerName = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "";
            ViewBag.CustomerPhone = User.FindFirst(ClaimTypes.MobilePhone)?.Value
                                   ?? User.FindFirst("PhoneNumber")?.Value
                                   ?? "";
        }

        // Đơn hàng của tôi (Chỉ lọc đúng UserId)
        public async Task<IActionResult> MyOrders()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                TempData["Message"] = "Không xác định được tài khoản đăng nhập";
                TempData["MessageType"] = "error";
                return View(new List<Order>());
            }

            var orders = await _context.Orders
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return View(orders);
        }

        // 🔒 Chỉ hiển thị hóa đơn của tài khoản đang đăng nhập
        public async Task<IActionResult> Invoice(string? phone)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out int userId);

            var orders = await _context.Orders
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            ViewBag.Phone = phone;
            return View(orders);
        }

        // 🔒 Trả partial hóa đơn đúng chính chủ
        [HttpGet]
        public async Task<IActionResult> GetInvoicePartial(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out int userId);

            var order = await _context.Orders
                .Include(x => x.OrderItems)
                    .ThenInclude(x => x.OrderItemToppings)
                .FirstOrDefaultAsync(x => x.OrderId == id && x.UserId == userId);

            if (order == null) return NotFound();

            return PartialView("_CustomerInvoicePartial", order);
        }

        // 🔒 Chi tiết đơn hàng chính chủ
        public async Task<IActionResult> Detail(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out int userId);

            var order = await _context.Orders
                .Include(x => x.OrderItems)
                    .ThenInclude(x => x.OrderItemToppings)
                .FirstOrDefaultAsync(x => x.OrderId == id && x.UserId == userId);

            if (order == null) return NotFound();

            return View(order);
        }
    }
}