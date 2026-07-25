using CafePos.Data;
using CafePos.Models;
using CafePos.Models.ViewModels;
using CafePos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CafePos.Areas.Admin.Controllers
{
    // 🔒 Phân quyền dành cho Nhân viên và Admin
    [Authorize(Roles = "Admin,Employee")]
    public class OrderController : Controller
    {
        private readonly CafePosDbContext _context;
        private readonly VnPayService _vnPayService;
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

        // GET: Order/Checkout/5
        public async Task<IActionResult> Checkout(int id)
        {
            var order = await _context.Orders
                .Include(x => x.Table)
                .Include(x => x.OrderItems)
                    .ThenInclude(x => x.OrderItemToppings)
                .FirstOrDefaultAsync(x => x.OrderId == id);

            if (order == null) return NotFound();

            // Lấy danh sách nhân viên có PositionId là 1 (Quản lý) hoặc 2 (Thu ngân)
            ViewBag.EmployeesList = await _context.Employees
                .Include(e => e.Position)
                .Where(e => e.IsActive && (e.PositionId == 1 || e.PositionId == 2))
                .ToListAsync();

            ViewBag.TablesList = await _context.Tables.Where(t => t.IsActive).ToListAsync();
            ViewBag.Toppings = await _context.Toppings.Where(x => x.IsActive).ToListAsync();

            return View(order);
        }

        // POST: Order/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(
            int id,
            List<int>? selectedToppingIds,
            decimal discountAmount,
            string paymentMethod,
            int? tableId,
            int? employeeId)
        {
            // Reload lại ViewBag nếu có lỗi validation
            ViewBag.EmployeesList = await _context.Employees.Include(e => e.Position).Where(e => e.IsActive && (e.PositionId == 1 || e.PositionId == 2)).ToListAsync();
            ViewBag.TablesList = await _context.Tables.Where(t => t.IsActive).ToListAsync();
            ViewBag.Toppings = await _context.Toppings.Where(x => x.IsActive).ToListAsync();

            var order = await _context.Orders
                .Include(x => x.OrderItems)
                    .ThenInclude(x => x.OrderItemToppings)
                .FirstOrDefaultAsync(x => x.OrderId == id);

            if (order == null) return NotFound();

            if (order.PaymentStatus == "Paid" || order.OrderStatus == "Completed")
            {
                TempData["Message"] = "Đơn hàng này đã được thanh toán rồi";
                TempData["MessageType"] = "warning";
                return RedirectToAction("Detail", new { id = order.OrderId });
            }

            // Validations
            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                TempData["Message"] = "Vui lòng chọn phương thức thanh toán!";
                TempData["MessageType"] = "error";
                return View(order);
            }

            if (!employeeId.HasValue || employeeId == 0)
            {
                TempData["Message"] = "Vui lòng chọn nhân viên đứng ca thực hiện!";
                TempData["MessageType"] = "error";
                return View(order);
            }

            // Lấy Nhân viên kèm Chức vụ (Position)
            var employee = await _context.Employees
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
            {
                TempData["Message"] = "Không tìm thấy thông tin nhân viên!";
                TempData["MessageType"] = "error";
                return View(order);
            }

            // Kiểm tra quyền: Chỉ cho phép Quản lý (1) hoặc Thu ngân (2)
            if (employee.PositionId != 1 && employee.PositionId != 2)
            {
                TempData["Message"] = $"Nhân viên {employee.FullName} ({employee.Position?.PositionName}) không có quyền thực hiện thanh toán!";
                TempData["MessageType"] = "error";
                return View(order);
            }

            // Bổ sung Topping chọn thêm tại quầy
            var firstItem = order.OrderItems?.FirstOrDefault();
            if (firstItem != null && selectedToppingIds != null && selectedToppingIds.Any())
            {
                if (firstItem.OrderItemToppings == null)
                    firstItem.OrderItemToppings = new List<OrderItemTopping>();

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

            // Tính lại tổng tiền
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

            string oldStatus = order.OrderStatus;
            order.SubTotal = subTotal;
            order.DiscountAmount = discountAmount < 0 ? 0 : discountAmount;
            order.TotalAmount = order.SubTotal - order.DiscountAmount;
            if (order.TotalAmount < 0) order.TotalAmount = 0;

            order.TableId = tableId;
            order.PaymentMethod = paymentMethod;

            // PHÂN LUỒNG VNPAY HOẶC TIỀN MẶT / BANKING
            if (paymentMethod.ToUpper() == "VNPAY")
            {
                await _context.SaveChangesAsync();

                // Lưu tạm employeeId vào Session để đọc lại trong Callback
                HttpContext.Session.SetInt32("VnPay_EmployeeId", employee.EmployeeId);

                var vnPayRequest = new VnPayPaymentRequest
                {
                    OrderId = order.OrderId,
                    Amount = order.TotalAmount,
                    OrderInfo = $"Thanh toan don hàng #{order.OrderCode} tai CafePos"
                };

                string paymentUrl = _vnPayService.CreatePaymentUrl(vnPayRequest, HttpContext);
                return Redirect(paymentUrl);
            }
            else
            {
                // Thanh toán trực tiếp tại quầy
                order.PaymentStatus = "Paid";
                order.OrderStatus = "Completed";

                _context.Payments.Add(new Payment
                {
                    OrderId = order.OrderId,
                    EmployeeId = employee.EmployeeId,
                    Method = paymentMethod,
                    Amount = order.TotalAmount,
                    PaidAt = DateTime.Now,
                    IsSuccess = true,
                    OrderInfo = $"Thanh toán bởi: {employee.FullName} ({employee.Position?.PositionName})"
                });

                _context.OrderStatusLogs.Add(new OrderStatusLog
                {
                    OrderId = order.OrderId,
                    OldStatus = oldStatus,
                    NewStatus = "Completed",
                    ChangedAt = DateTime.Now,
                    ChangedBy = $"{employee.FullName} ({employee.Position?.PositionName})"
                });

                await _context.SaveChangesAsync();

                TempData["Message"] = "Checkout thành công!";
                TempData["MessageType"] = "success";

                return RedirectToAction("Detail", new { id = order.OrderId });
            }
        }

        // GET: Order/PaymentCallback (Nhận phản hồi từ VNPay)
        public async Task<IActionResult> PaymentCallback()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (!int.TryParse(response.OrderCode, out int orderId))
            {
                TempData["Message"] = "Không xác định được mã đơn hàng từ VNPay!";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index");
            }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null) return NotFound();

            if (response.Success && response.ResponseCode == "00")
            {
                // Lấy Mã nhân viên thực hiện từ Session
                int? employeeId = HttpContext.Session.GetInt32("VnPay_EmployeeId");
                var employee = await _context.Employees.Include(e => e.Position).FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
                string staffInfo = employee != null ? $"{employee.FullName} ({employee.Position?.PositionName})" : "Thu ngân (VNPay)";

                string oldStatus = order.OrderStatus;
                order.PaymentStatus = "Paid";
                order.OrderStatus = "Completed";

                _context.Payments.Add(new Payment
                {
                    OrderId = order.OrderId,
                    EmployeeId = employeeId,
                    Method = "VNPAY",
                    Amount = response.Amount,
                    TransactionNo = response.TransactionNo,
                    ResponseCode = response.ResponseCode,
                    OrderInfo = response.OrderInfo,
                    IsSuccess = true,
                    PaidAt = DateTime.Now
                });

                _context.OrderStatusLogs.Add(new OrderStatusLog
                {
                    OrderId = order.OrderId,
                    OldStatus = oldStatus,
                    NewStatus = "Completed",
                    ChangedAt = DateTime.Now,
                    ChangedBy = staffInfo
                });

                await _context.SaveChangesAsync();

                TempData["Message"] = "Thanh toán qua VNPAY thành công!";
                TempData["MessageType"] = "success";
            }
            else
            {
                TempData["Message"] = $"Thanh toán VNPAY không thành công. Mã lỗi: {response.ResponseCode}";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction("Detail", new { id = orderId });
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
        // GET: Admin/Order/GetActiveCashiers
        [HttpGet]
        public async Task<IActionResult> GetActiveCashiers()
        {
            var cashiers = await _context.Employees
                .Include(e => e.Position)
                .Where(e => e.IsActive && (e.PositionId == 1 || e.PositionId == 2))
                .Select(e => new
                {
                    employeeId = e.EmployeeId,
                    fullName = e.FullName,
                    positionName = e.Position != null ? e.Position.PositionName : "N/A"
                })
                .ToListAsync();

            return Json(cashiers);
        }

        // POST: Admin/Order/MarkAsPaid
        [HttpPost]
        public async Task<IActionResult> MarkAsPaid(int id, string paymentMethod, int employeeId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null) return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });

            var employee = await _context.Employees.Include(e => e.Position).FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
            if (employee == null || (employee.PositionId != 1 && employee.PositionId != 2))
            {
                return Json(new { success = false, message = "Nhân viên không có quyền thực hiện thanh toán!" });
            }

            string oldStatus = order.OrderStatus;
            order.PaymentStatus = "Paid";
            order.OrderStatus = "Completed";
            order.PaymentMethod = string.IsNullOrEmpty(paymentMethod) ? "Cash" : paymentMethod;

            _context.Payments.Add(new Payment
            {
                OrderId = order.OrderId,
                EmployeeId = employee.EmployeeId,
                Method = order.PaymentMethod,
                Amount = order.TotalAmount,
                PaidAt = DateTime.Now,
                IsSuccess = true,
                OrderInfo = $"Thanh toán nhanh bởi: {employee.FullName} ({employee.Position?.PositionName})"
            });

            _context.OrderStatusLogs.Add(new OrderStatusLog
            {
                OrderId = order.OrderId,
                OldStatus = oldStatus,
                NewStatus = "Completed",
                ChangedAt = DateTime.Now,
                ChangedBy = $"{employee.FullName} ({employee.Position?.PositionName})"
            });

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }

}