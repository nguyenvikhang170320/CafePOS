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
    [Authorize(Roles ="Staff")]
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
            List<int> productIds,
            List<int> quantities,
            List<int?>? productSizeIds,
            List<int>? selectedToppingIds)
        {
            await LoadCreateViewData();

            var customerName = User.FindFirst("FullName")?.Value ?? User.Identity?.Name;
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

            var validItems = new List<OrderItem>();
            decimal subTotal = 0;

            List<Topping> selectedToppings = new();
            if (selectedToppingIds != null && selectedToppingIds.Any())
            {
                selectedToppings = await _context.Toppings
                    .Where(x => selectedToppingIds.Contains(x.ToppingId) && x.IsActive)
                    .ToListAsync();
            }

            for (int i = 0; i < productIds.Count; i++)
            {
                if (i >= quantities.Count) continue;
                int quantity = quantities[i];
                if (quantity <= 0) continue;

                var product = await _context.Products.FirstOrDefaultAsync(x => x.ProductId == productIds[i] && x.IsActive);
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
                    OrderItemToppings = new List<OrderItemTopping>()
                };

                decimal toppingTotal = 0;
                foreach (var top in selectedToppings)
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

                orderItem.LineTotal = (orderItem.UnitPrice * quantity) + toppingTotal;
                subTotal += orderItem.LineTotal;
                validItems.Add(orderItem);
            }

            if (!validItems.Any())
            {
                TempData["Message"] = "Vui lòng chọn ít nhất 1 món với số lượng lớn hơn 0";
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

            ViewBag.CustomerName = User.FindFirst("FullName")?.Value ?? User.Identity?.Name;
            ViewBag.CustomerPhone = User.FindFirst("PhoneNumber")?.Value;
        }

        public async Task<IActionResult> MyOrders()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdClaim, out int userId);

            var orders = await _context.Orders
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return View(orders);
        }

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