using CafePos.Data;
using CafePos.Models;
using CafePos.Models.ViewModels;
using CafePos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace CafePos.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PaymentController : Controller
    {
        private readonly CafePosDbContext _context;
        private readonly IVnPayService _vnPayService;

        public PaymentController(CafePosDbContext context, IVnPayService vnPayService)
        {
            _context = context;
            _vnPayService = vnPayService;
        }

        // POST: Admin/Payment/CreateVnPayPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVnPayPayment(int id, int? employeeId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == id);
            if (order == null)
            {
                TempData["Message"] = "Không tìm thấy đơn hàng cần thanh toán!";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", "Order");
            }

            if (order.PaymentStatus == "Paid")
            {
                TempData["Message"] = "Đơn hàng này đã được thanh toán trước đó rồi!";
                TempData["MessageType"] = "warning";
                return RedirectToAction("Detail", "Order", new { id = order.OrderId });
            }

            // Lưu EmployeeId trực ca vào Session để khi VNPay Callback trở lại sẽ lấy ra lưu vết
            if (employeeId.HasValue && employeeId.Value > 0)
            {
                HttpContext.Session.SetInt32("VnPay_EmployeeId", employeeId.Value);
            }

            var paymentRequest = new VnPayPaymentRequest
            {
                OrderId = order.OrderId,
                Amount = order.TotalAmount,
                OrderInfo = $"Thanh toan don hang {order.OrderCode} tai CafePos",
                Language = "vn"
            };

            string paymentUrl = _vnPayService.CreatePaymentUrl(paymentRequest, HttpContext);
            return Redirect(paymentUrl);
        }

        // GET: Admin/Payment/VnPayReturn
        [HttpGet]
        public async Task<IActionResult> VnPayReturn()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (!response.Success)
            {
                TempData["Message"] = "Lỗi xác thực chữ ký an toàn từ VNPAY!";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", "Order");
            }

            if (!int.TryParse(response.OrderCode, out int orderId))
            {
                TempData["Message"] = "Mã đơn hàng phản hồi từ VNPAY không hợp lệ!";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index", "Order");
            }

            var order = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == orderId);
            if (order == null) return NotFound();

            // Đọc Mã nhân viên thực hiện từ Session (đã lưu ở bước CreateVnPayPayment)
            int? employeeId = HttpContext.Session.GetInt32("VnPay_EmployeeId");
            var employee = await _context.Employees.Include(e => e.Position).FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
            string staffInfo = employee != null ? $"{employee.FullName} ({employee.Position?.PositionName})" : "Cổng VNPAY";

            string oldStatus = order.OrderStatus; // Lưu lại trạng thái cũ trước khi đổi

            // Nếu VNPAY báo thành công (ResponseCode == "00")
            if (response.ResponseCode == "00")
            {
                // 1. Cập nhật bảng Orders
                order.PaymentStatus = "Paid";
                order.OrderStatus = "Completed";
                order.PaymentMethod = "VNPAY";

                // 2. Lưu vết giao dịch vào bảng Payments (Đã sửa từ VnPayPaymentResponse -> Payment)
                var paymentRecord = new Payment
                {
                    OrderId = order.OrderId,
                    EmployeeId = employeeId,
                    Method = "VNPAY",
                    Amount = response.Amount > 0 ? response.Amount : order.TotalAmount,
                    PaidAt = DateTime.Now,
                    TransactionNo = response.TransactionNo,
                    ResponseCode = response.ResponseCode,
                    OrderInfo = response.OrderInfo,
                    IsSuccess = true
                };
                _context.Payments.Add(paymentRecord);

                // 3. Ghi log lịch sử trạng thái
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
                return RedirectToAction("Detail", "Order", new { id = order.OrderId });
            }
            else
            {
                // Thanh toán thất bại hoặc khách hủy giao dịch
                var paymentRecord = new Payment
                {
                    OrderId = order.OrderId,
                    EmployeeId = employeeId,
                    Method = "VNPAY",
                    Amount = order.TotalAmount,
                    PaidAt = DateTime.Now,
                    TransactionNo = response.TransactionNo,
                    ResponseCode = response.ResponseCode,
                    OrderInfo = $"Giao dịch thất bại: {response.OrderInfo}",
                    IsSuccess = false
                };
                _context.Payments.Add(paymentRecord);
                await _context.SaveChangesAsync();

                TempData["Message"] = $"Thanh toán VNPAY không thành công. Mã lỗi: {response.ResponseCode}";
                TempData["MessageType"] = "error";
                return RedirectToAction("Checkout", "Order", new { id = order.OrderId });
            }
        }
    }
}