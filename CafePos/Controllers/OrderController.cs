using CafePos.Data;
using CafePos.Models;
using CafePos.Models.ViewModels;
using CafePos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafePos.Models.Settings;
using Microsoft.Extensions.Options;
namespace CafePos.Controllers
{
    // 🔒 Phân quyền dành cho Nhân viên và Admin
    [Authorize(Roles = "Admin,Employee")]
    public class OrderController : Controller
    {
        private readonly CafePosDbContext _context;
        private readonly IVnPayService _vnPayService;
        private readonly BankingSettings _bankingSettings;
        public OrderController(CafePosDbContext context, IVnPayService vnPayService, IOptions<BankingSettings> bankingOptions)
        {
            _context = context;
            _vnPayService = vnPayService;
            _bankingSettings = bankingOptions.Value;
        }
        // =========================================================
        // LẤY NHÂN VIÊN ĐANG ĐĂNG NHẬP
        // =========================================================
        private async Task<Employee?> GetCurrentEmployee()
        {
            var username = User.Identity?.Name?.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(
                    x => x.Username == username
                );

            if (user == null)
            {
                return null;
            }

            return await _context.Employees
                .Include(x => x.Position)
                .FirstOrDefaultAsync(
                    x => x.UserId == user.UserId
                );
        }

        // =========================================================
        // KIỂM TRA QUYỀN THANH TOÁN
        // Admin        => Có quyền
        // Quản lý (2)  => Có quyền
        // Thu ngân (3) => Có quyền
        // Phục vụ (5)  => Có quyền
        // Pha chế (4)  => Không có quyền
        // =========================================================
        private bool CanProcessPayment(Employee? employee)
        {
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            if (employee == null)
            {
                return false;
            }

            return employee.PositionId == 2 ||
                   employee.PositionId == 3 ||
                   employee.PositionId == 5;
        }

        // =========================================================
        // DANH SÁCH NHÂN VIÊN ĐƯỢC PHÉP GHI NHẬN THANH TOÁN
        // Admin (PositionId = 1) chỉ xuất hiện khi tài khoản hiện tại là Admin.
        // Employee hợp lệ: Quản lý (2), Thu ngân (3), Phục vụ (5).
        // =========================================================
        private async Task<List<Employee>> GetPaymentEmployeesAsync()
        {
            bool isAdmin = User.IsInRole("Admin");

            return await _context.Employees
                .Include(e => e.Position)
                .Where(e =>
                    e.IsActive &&
                    (
                        e.PositionId == 2 ||
                        e.PositionId == 3 ||
                        e.PositionId == 5 ||
                        (isAdmin && e.PositionId == 1)
                    ))
                .ToListAsync();
        }

        private bool CanBeSelectedForPayment(Employee employee)
        {
            if (employee.PositionId == 2 ||
                employee.PositionId == 3 ||
                employee.PositionId == 5)
            {
                return true;
            }

            return User.IsInRole("Admin") && employee.PositionId == 1;
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
        // ================================================================
        // POS - NHÂN VIÊN TẠO ORDER TẠI QUÁN
        // GET: /Order/POS
        // ================================================================
        [HttpGet]
        public async Task<IActionResult> POS()
        {
            var currentEmployee = await GetCurrentEmployee();

            // Admin hoặc nhân viên hợp lệ mới được dùng POS.
            if (!CanProcessPayment(currentEmployee))
            {
                TempData["Message"] =
                    "Bạn không có quyền sử dụng màn hình POS.";

                TempData["MessageType"] =
                    "error";

                return RedirectToAction(nameof(Index));
            }

            // ============================================================
            // DANH SÁCH BÀN
            // ============================================================
            ViewBag.Tables = await _context.Tables
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Area)
                .ThenBy(x => x.TableName)
                .ToListAsync();

            // ============================================================
            // DANH SÁCH SẢN PHẨM + SIZE
            // ============================================================
            ViewBag.Products = await _context.Products
                .AsNoTracking()
                .Include(x => x.ProductSizes)
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            // ============================================================
            // TOPPING
            // ============================================================
            ViewBag.Toppings = await _context.Toppings
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return View(new PosOrderViewModel());
        }


        // ================================================================
        // POS - TẠO ORDER TẠI QUÁN
        // POST: /Order/POS
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> POS(
            PosOrderViewModel model)
        {
            // ============================================================
            // 1. KIỂM TRA QUYỀN NHÂN VIÊN
            // ============================================================
            var currentEmployee =
                await GetCurrentEmployee();

            if (!CanProcessPayment(currentEmployee))
            {
                TempData["Message"] =
                    "Bạn không có quyền tạo order tại POS.";

                TempData["MessageType"] =
                    "error";

                return RedirectToAction(nameof(Index));
            }


            // ============================================================
            // 2. LẤY USER ĐANG ĐĂNG NHẬP
            //
            // Orders.UserId của database hiện tại là NOT NULL.
            // ============================================================
            var username =
                User.Identity?.Name?.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                return Forbid();
            }


            var currentUser =
                await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.Username == username
                    );


            if (currentUser == null)
            {
                TempData["Message"] =
                    "Không xác định được tài khoản đang đăng nhập.";

                TempData["MessageType"] =
                    "error";

                return RedirectToAction(nameof(POS));
            }


            // ============================================================
            // 3. KIỂM TRA MÓN
            // ============================================================
            if (model.Items == null ||
                !model.Items.Any(x => x.Quantity > 0))
            {
                TempData["Message"] =
                    "Vui lòng chọn ít nhất một món.";

                TempData["MessageType"] =
                    "warning";

                return RedirectToAction(nameof(POS));
            }


            // ============================================================
            // 4. KIỂM TRA BÀN
            // ============================================================
            if (model.TableId.HasValue)
            {
                var table =
                    await _context.Tables
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.TableId == model.TableId.Value &&
                            x.IsActive
                        );


                if (table == null)
                {
                    TempData["Message"] =
                        "Bàn không tồn tại hoặc đã ngừng hoạt động.";

                    TempData["MessageType"] =
                        "error";

                    return RedirectToAction(nameof(POS));
                }


                // ========================================================
                // Bàn đã có đơn Pending + Unpaid
                // thì không tạo thêm order mới.
                // ========================================================
                var existingOrder =
                    await _context.Orders
                        .AsNoTracking()
                        .Where(x =>
                            x.TableId == model.TableId.Value &&
                            x.OrderStatus == "Pending" &&
                            x.PaymentStatus == "Unpaid"
                        )
                        .OrderByDescending(x =>
                            x.CreatedDate
                        )
                        .FirstOrDefaultAsync();


                if (existingOrder != null)
                {
                    TempData["Message"] =
                        $"Bàn này đang có đơn #{existingOrder.OrderId}. " +
                        "Vui lòng mở đơn hiện tại để thêm món.";

                    TempData["MessageType"] =
                        "warning";


                    return RedirectToAction(
                        nameof(Detail),
                        new
                        {
                            id = existingOrder.OrderId
                        }
                    );
                }
            }


            // ============================================================
            // 5. TRANSACTION
            // ============================================================
            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();


            try
            {
                var now =
                    DateTime.Now;


                // ========================================================
                // 6. TẠO ORDER
                // ========================================================
                var order =
                    new Order
                    {
                        OrderCode =
                            $"CPOS{now:yyyyMMdd-HHmmss}",

                        CreatedDate =
                            now,

                        UserId =
                            currentUser.UserId,

                        TableId =
                            model.TableId,

                        CustomerName =
                            "Khách tại quán",

                        CustomerPhone =
                            null,

                        Note =
                            model.Note,

                        OrderStatus =
                            "Pending",

                        PaymentStatus =
                            "Unpaid",

                        PaymentMethod =
                            null,

                        SubTotal =
                            0,

                        DiscountAmount =
                            0,

                        TotalAmount =
                            0
                    };


                _context.Orders.Add(order);

                await _context.SaveChangesAsync();


                decimal subTotal = 0;


                // ========================================================
                // 7. TẠO ORDER ITEMS
                // ========================================================
                foreach (var input in model.Items)
                {
                    if (input.Quantity <= 0)
                    {
                        continue;
                    }


                    // ====================================================
                    // PRODUCT
                    // ====================================================
                    var product =
                        await _context.Products
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x =>
                                x.ProductId ==
                                    input.ProductId
                                &&
                                x.IsActive
                            );


                    if (product == null)
                    {
                        throw new InvalidOperationException(
                            $"Sản phẩm ID {input.ProductId} " +
                            "không tồn tại hoặc đã ngừng bán."
                        );
                    }


                    decimal unitPrice =
                        product.BasePrice;


                    ProductSize? selectedSize =
                        null;


                    // ====================================================
                    // SIZE
                    // ====================================================
                    if (input.ProductSizeId.HasValue)
                    {
                        selectedSize =
                            await _context.ProductSizes
                                .AsNoTracking()
                                .FirstOrDefaultAsync(x =>
                                    x.ProductSizeId ==
                                        input.ProductSizeId.Value
                                    &&
                                    x.ProductId ==
                                        product.ProductId
                                    &&
                                    !x.IsDeleted
                                );


                        if (selectedSize == null)
                        {
                            throw new InvalidOperationException(
                                $"Size của món '{product.Name}' không hợp lệ."
                            );
                        }


                        unitPrice +=
                            selectedSize.ExtraPrice;
                    }


                    // ====================================================
                    // TOPPING
                    // ====================================================
                    var toppings =
                        new List<Topping>();


                    if (input.ToppingIds != null &&
                        input.ToppingIds.Any())
                    {
                        var toppingIds =
                            input.ToppingIds
                                .Distinct()
                                .ToList();


                        toppings =
                            await _context.Toppings
                                .AsNoTracking()
                                .Where(x =>
                                    toppingIds.Contains(
                                        x.ToppingId
                                    )
                                    &&
                                    x.IsActive
                                )
                                .ToListAsync();


                        if (toppings.Count !=
                            toppingIds.Count)
                        {
                            throw new InvalidOperationException(
                                $"Có topping của món '{product.Name}' " +
                                "không tồn tại hoặc đã ngừng sử dụng."
                            );
                        }
                    }


                    // ====================================================
                    // TÍNH TIỀN
                    //
                    // UnitPrice:
                    // BasePrice + ExtraPrice của size
                    //
                    // Topping:
                    // lưu riêng trong OrderItemToppings
                    // ====================================================
                    decimal productTotal =
                        unitPrice *
                        input.Quantity;


                    decimal toppingTotal =
                        toppings.Sum(x =>
                            x.Price *
                            input.Quantity
                        );


                    decimal lineTotal =
                        productTotal +
                        toppingTotal;


                    // ====================================================
                    // ORDER ITEM
                    // ====================================================
                    var orderItem =
                        new OrderItem
                        {
                            OrderId =
                                order.OrderId,

                            ProductId =
                                product.ProductId,

                            ProductNameSnapshot =
                                product.Name,

                            ProductSizeId =
                                selectedSize?.ProductSizeId,

                            SizeNameSnapshot =
                                selectedSize?.SizeName,

                            Quantity =
                                input.Quantity,

                            UnitPrice =
                                unitPrice,

                            ItemNote =
                                input.Note,

                            LineTotal =
                                lineTotal
                        };


                    _context.OrderItems.Add(
                        orderItem
                    );


                    // Cần OrderItemId trước khi thêm topping.
                    await _context.SaveChangesAsync();


                    // ====================================================
                    // ORDER ITEM TOPPING
                    // ====================================================
                    foreach (var topping in toppings)
                    {
                        _context.OrderItemToppings.Add(
                            new OrderItemTopping
                            {
                                OrderItemId =
                                    orderItem.OrderItemId,

                                ToppingId =
                                    topping.ToppingId,

                                ToppingNameSnapshot =
                                    topping.Name,

                                Price =
                                    topping.Price,

                                Quantity =
                                    input.Quantity,

                                TotalPrice =
                                    topping.Price *
                                    input.Quantity
                            }
                        );
                    }


                    subTotal +=
                        lineTotal;
                }


                // ========================================================
                // 8. PHẢI CÓ ITEM HỢP LỆ
                // ========================================================
                if (subTotal <= 0)
                {
                    throw new InvalidOperationException(
                        "Order không có món hợp lệ."
                    );
                }


                // ========================================================
                // 9. TÍNH TỔNG ORDER
                // ========================================================
                order.SubTotal =
                    subTotal;

                order.DiscountAmount =
                    0;

                order.TotalAmount =
                    order.SubTotal;


                // ========================================================
                // 10. GHI LOG
                // ========================================================
                string changedBy;


                if (currentEmployee != null)
                {
                    changedBy =
                        $"{currentEmployee.FullName} " +
                        $"({currentEmployee.Position?.PositionName})";
                }
                else
                {
                    changedBy =
                        User.Identity?.Name
                        ?? "Admin";
                }


                _context.OrderStatusLogs.Add(
                    new OrderStatusLog
                    {
                        OrderId =
                            order.OrderId,

                        OldStatus =
                            string.Empty,

                        NewStatus =
                            "Pending",

                        ChangedAt =
                            now,

                        ChangedBy =
                            changedBy
                    }
                );


                // ========================================================
                // 11. SAVE + COMMIT
                // ========================================================
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();


                TempData["Message"] =
                    $"Đã tạo order #{order.OrderId} thành công.";

                TempData["MessageType"] =
                    "success";


                // ========================================================
                // 12. TỰ CHUYỂN SANG INDEX
                // ========================================================
                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        id = order.OrderId
                    }
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();


                TempData["Message"] =
                    ex.InnerException?.Message
                    ?? ex.Message;

                TempData["MessageType"] =
                    "error";


                return RedirectToAction(
                    nameof(POS)
                );
            }
        }
        // 3. Chi tiết hóa đơn full page
        // =========================================================
        // CHI TIẾT HÓA ĐƠN
        // Có thêm thông tin nhân viên đã thanh toán
        // =========================================================
        public async Task<IActionResult> Detail(int id)
        {
            var order = await _context.Orders
                .Include(x => x.Table)
                .Include(x => x.OrderItems)
                    .ThenInclude(x => x.OrderItemToppings)
                .FirstOrDefaultAsync(x => x.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            // =====================================================
            // Lấy giao dịch thanh toán thành công gần nhất
            // =====================================================
            var payment = await _context.Payments
                .AsNoTracking()
                .Where(x =>
                    x.OrderId == id &&
                    x.IsSuccess
                )
                .OrderByDescending(x => x.PaidAt)
                .FirstOrDefaultAsync();

            ViewBag.Payment = payment;

            // =====================================================
            // Lấy nhân viên đã thực hiện thanh toán
            // =====================================================
            if (payment != null)
            {
                var employee = await _context.Employees
                    .AsNoTracking()
                    .Include(x => x.Position)
                    .FirstOrDefaultAsync(
                        x => x.EmployeeId == payment.EmployeeId
                    );

                ViewBag.PaymentEmployee = employee;
            }
            else
            {
                ViewBag.PaymentEmployee = null;
            }

            return View(order);
        }

        // GET: Order/Checkout/5
        public async Task<IActionResult> Checkout(int id)
        {
            var order = await _context.Orders
                .Include(x => x.Table)
                .Include(x => x.OrderItems)
                    .ThenInclude(x => x.OrderItemToppings)
                .Include(x => x.Payments)
                .FirstOrDefaultAsync(x => x.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            var currentEmployee = await GetCurrentEmployee();

            if (!CanProcessPayment(currentEmployee))
            {
                TempData["Message"] =
                    "Bạn không có quyền thực hiện thanh toán.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Detail", new { id });
            }

            if (order.OrderStatus == "Cancelled" ||
                order.PaymentStatus == "Cancelled")
            {
                TempData["Message"] =
                    "Đơn hàng đã bị hủy, không thể thanh toán.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Detail", new { id });
            }

            if (order.OrderStatus == "Completed" ||
                order.PaymentStatus == "Paid")
            {
                TempData["Message"] =
                    "Đơn hàng này đã được thanh toán rồi.";
                TempData["MessageType"] = "warning";
                return RedirectToAction("Detail", new { id });
            }

            var employeesList = await GetPaymentEmployeesAsync();
            ViewBag.EmployeesList = employeesList;

            int? currentEmployeeId = null;
            if (currentEmployee != null &&
                employeesList.Any(e => e.EmployeeId == currentEmployee.EmployeeId))
            {
                currentEmployeeId = currentEmployee.EmployeeId;
            }

            int? savedEmployeeId = order.Payments?
                .Where(p => p.IsSuccess)
                .OrderByDescending(p => p.PaidAt)
                .Select(p => (int?)p.EmployeeId)
                .FirstOrDefault();

            ViewBag.SelectedEmployeeId = savedEmployeeId ?? currentEmployeeId;
            ViewBag.TablesList = await _context.Tables
                .Where(t => t.IsActive)
                .ToListAsync();
            ViewBag.Toppings = await _context.Toppings
                .Where(x => x.IsActive)
                .ToListAsync();
            ViewBag.BankId = _bankingSettings.BankId;
            ViewBag.BankAccountNumber = _bankingSettings.AccountNumber;
            ViewBag.BankAccountName = _bankingSettings.AccountName;
            ViewBag.CanPay = true;

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
            ViewBag.EmployeesList = await GetPaymentEmployeesAsync();
            ViewBag.TablesList = await _context.Tables
                .Where(t => t.IsActive)
                .ToListAsync();
            ViewBag.Toppings = await _context.Toppings
                .Where(x => x.IsActive)
                .ToListAsync();
            ViewBag.BankId = _bankingSettings.BankId;
            ViewBag.BankAccountNumber = _bankingSettings.AccountNumber;
            ViewBag.BankAccountName = _bankingSettings.AccountName;

            var currentEmployee = await GetCurrentEmployee();

            if (!CanProcessPayment(currentEmployee))
            {
                TempData["Message"] =
                    "Chỉ Admin, Quản lý, Thu ngân hoặc Phục vụ mới được thực hiện thanh toán.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Detail", new { id });
            }

            var order = await _context.Orders
                .Include(x => x.OrderItems)
                    .ThenInclude(x => x.OrderItemToppings)
                .FirstOrDefaultAsync(x => x.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            if (order.OrderStatus == "Cancelled" ||
                order.PaymentStatus == "Cancelled")
            {
                TempData["Message"] =
                    "Đơn hàng đã bị hủy, không thể thanh toán.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Detail", new { id = order.OrderId });
            }

            if (order.PaymentStatus == "Paid" ||
                order.OrderStatus == "Completed")
            {
                TempData["Message"] =
                    "Đơn hàng này đã được thanh toán rồi.";
                TempData["MessageType"] = "warning";
                return RedirectToAction("Detail", new { id = order.OrderId });
            }

            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                TempData["Message"] = "Vui lòng chọn phương thức thanh toán!";
                TempData["MessageType"] = "error";
                return View(order);
            }

            var allowedMethods = new[] { "Cash", "Banking", "Card", "VNPAY" };
            var normalizedMethod = allowedMethods.FirstOrDefault(x =>
                x.Equals(paymentMethod.Trim(), StringComparison.OrdinalIgnoreCase));

            if (normalizedMethod == null)
            {
                TempData["Message"] = "Phương thức thanh toán không hợp lệ!";
                TempData["MessageType"] = "error";
                return View(order);
            }

            paymentMethod = normalizedMethod;

            if (!employeeId.HasValue || employeeId.Value <= 0)
            {
                TempData["Message"] = "Vui lòng chọn nhân viên đứng ca thực hiện!";
                TempData["MessageType"] = "error";
                return View(order);
            }

            var employee = await _context.Employees
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId.Value && e.IsActive);

            if (employee == null)
            {
                TempData["Message"] = "Không tìm thấy thông tin nhân viên!";
                TempData["MessageType"] = "error";
                return View(order);
            }

            if (!CanBeSelectedForPayment(employee))
            {
                TempData["Message"] =
                    $"Nhân viên {employee.FullName} ({employee.Position?.PositionName}) không có quyền thực hiện thanh toán!";
                TempData["MessageType"] = "error";
                return View(order);
            }

            var firstItem = order.OrderItems?.FirstOrDefault();
            if (firstItem != null &&
                selectedToppingIds != null &&
                selectedToppingIds.Any())
            {
                firstItem.OrderItemToppings ??= new List<OrderItemTopping>();

                var toppings = await _context.Toppings
                    .Where(x => selectedToppingIds.Contains(x.ToppingId) && x.IsActive)
                    .ToListAsync();

                foreach (var topping in toppings)
                {
                    bool existed = firstItem.OrderItemToppings
                        .Any(x => x.ToppingId == topping.ToppingId);

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

            decimal subTotal = 0;
            if (order.OrderItems != null)
            {
                foreach (var item in order.OrderItems)
                {
                    decimal toppingTotal = item.OrderItemToppings?
                        .Sum(x => x.TotalPrice) ?? 0;

                    item.LineTotal = (item.UnitPrice * item.Quantity) + toppingTotal;
                    subTotal += item.LineTotal;
                }
            }

            string oldStatus = order.OrderStatus;
            order.SubTotal = subTotal;
            order.DiscountAmount = discountAmount < 0 ? 0 : discountAmount;
            order.TotalAmount = order.SubTotal - order.DiscountAmount;
            if (order.TotalAmount < 0)
            {
                order.TotalAmount = 0;
            }

            order.TableId = tableId;
            order.PaymentMethod = paymentMethod;

            if (paymentMethod.Equals("VNPAY", StringComparison.OrdinalIgnoreCase))
            {
                await _context.SaveChangesAsync();

                HttpContext.Session.SetInt32(
                    "VnPay_EmployeeId",
                    employee.EmployeeId);

                var vnPayRequest = new VnPayPaymentRequest
                {
                    OrderId = order.OrderId,
                    Amount = order.TotalAmount,
                    OrderInfo = $"Thanh toan don hang #{order.OrderCode} tai CafePOS"
                };

                string paymentUrl = _vnPayService
                    .CreatePaymentUrl(vnPayRequest, HttpContext);

                return Redirect(paymentUrl);
            }

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
                OrderInfo =
                    $"Thanh toán bởi: {employee.FullName} ({employee.Position?.PositionName})"
            });

            _context.OrderStatusLogs.Add(new OrderStatusLog
            {
                OrderId = order.OrderId,
                OldStatus = oldStatus,
                NewStatus = "Completed",
                ChangedAt = DateTime.Now,
                ChangedBy =
                    $"{employee.FullName} ({employee.Position?.PositionName})"
            });

            await _context.SaveChangesAsync();

            TempData["Message"] = "Checkout thành công!";
            TempData["MessageType"] = "success";

            return RedirectToAction("Detail", new { id = order.OrderId });
        }

        // GET: Order/PaymentCallback (Nhận phản hồi từ VNPay)
        [AllowAnonymous]
        public async Task<IActionResult> PaymentCallback()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (!int.TryParse(response.OrderCode, out int orderId))
            {
                TempData["Message"] = "Không xác định được mã đơn hàng từ VNPay!";
                TempData["MessageType"] = "error";

                // Nếu không lấy được orderId, điều hướng an toàn theo Role
                if (User.IsInRole("Customer"))
                    return RedirectToAction("Index", "Home");

                return RedirectToAction("Index", "Order");
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

                // Xóa Session sau khi thanh toán thành công
                HttpContext.Session.Remove("VnPay_EmployeeId");

                TempData["Message"] = "Thanh toán qua VNPAY thành công!";
                TempData["MessageType"] = "success";
            }
            else
            {
                TempData["Message"] = $"Thanh toán VNPAY không thành công. Mã lỗi: {response.ResponseCode}";
                TempData["MessageType"] = "error";
            }

            // ==========================================
            // 🔀 PHÂN LUỒNG ĐIỀU HƯỚNG THEO ROLE NGƯỜI DÙNG
            // ==========================================

            // 1. Nếu là Khách hàng -> Về trang chi tiết đơn hàng của Khách hàng
            if (User.IsInRole("Customer"))
            {
                return RedirectToAction("Detail", "CustomerOrder", new { area = "", id = orderId });
            }

            // 2. Nếu là Nhân viên -> Về trang Detail ngoài Area Admin (hoặc trang Order của Nhân viên)
            if (User.IsInRole("Employee"))
            {
                return RedirectToAction("Detail", "Order", new { area = "", id = orderId });
            }

            // 3. Mặc định dành cho Admin -> Về trang Detail trong Area Admin
            return RedirectToAction("Detail", "Order", new { area = "Admin", id = orderId });
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

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.BankId = _bankingSettings.BankId;
            ViewBag.BankAccountNumber = _bankingSettings.AccountNumber;
            ViewBag.BankAccountName = _bankingSettings.AccountName;

            var currentEmployee = await GetCurrentEmployee();
            ViewBag.CanPay = CanProcessPayment(currentEmployee);

            return PartialView("_InvoicePartial", order);
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
            var currentEmployee = await GetCurrentEmployee();

            if (!CanProcessPayment(currentEmployee))
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = "Bạn không có quyền lấy danh sách nhân viên thanh toán."
                });
            }

            var employees = await GetPaymentEmployeesAsync();

            var result = employees
                .Select(e => new
                {
                    employeeId = e.EmployeeId,
                    fullName = e.FullName,
                    positionName = e.Position != null
                        ? e.Position.PositionName
                        : "N/A"
                })
                .ToList();

            return Json(result);
        }

        // POST: Admin/Order/MarkAsPaid
        [HttpPost]
        public async Task<IActionResult> MarkAsPaid(
            int id,
            string paymentMethod,
            int employeeId)
        {
            var currentEmployee = await GetCurrentEmployee();

            if (!CanProcessPayment(currentEmployee))
            {
                return Json(new
                {
                    success = false,
                    message = "Bạn không có quyền thực hiện thanh toán."
                });
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy đơn hàng!"
                });
            }

            if (order.OrderStatus == "Cancelled" ||
                order.PaymentStatus == "Cancelled")
            {
                return Json(new
                {
                    success = false,
                    message = "Đơn hàng đã bị hủy, không thể thanh toán."
                });
            }

            if (order.PaymentStatus == "Paid" ||
                order.OrderStatus == "Completed")
            {
                return Json(new
                {
                    success = false,
                    message = "Đơn hàng này đã được thanh toán rồi."
                });
            }

            var allowedMethods = new[] { "Cash", "Banking", "Card", "VNPAY" };
            var normalizedMethod = allowedMethods.FirstOrDefault(x =>
                x.Equals(paymentMethod?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (normalizedMethod == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Phương thức thanh toán không hợp lệ."
                });
            }

            var employee = await _context.Employees
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e =>
                    e.EmployeeId == employeeId &&
                    e.IsActive);

            if (employee == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy thông tin nhân viên!"
                });
            }

            if (!CanBeSelectedForPayment(employee))
            {
                return Json(new
                {
                    success = false,
                    message =
                        $"Nhân viên {employee.FullName} ({employee.Position?.PositionName}) không có quyền thực hiện thanh toán!"
                });
            }

            string oldStatus = order.OrderStatus;

            order.PaymentStatus = "Paid";
            order.OrderStatus = "Completed";
            order.PaymentMethod = normalizedMethod;

            _context.Payments.Add(new Payment
            {
                OrderId = order.OrderId,
                EmployeeId = employee.EmployeeId,
                Method = normalizedMethod,
                Amount = order.TotalAmount,
                PaidAt = DateTime.Now,
                IsSuccess = true,
                OrderInfo =
                    $"Thanh toán nhanh bởi: {employee.FullName} ({employee.Position?.PositionName})"
            });

            _context.OrderStatusLogs.Add(new OrderStatusLog
            {
                OrderId = order.OrderId,
                OldStatus = oldStatus,
                NewStatus = "Completed",
                ChangedAt = DateTime.Now,
                ChangedBy =
                    $"{employee.FullName} ({employee.Position?.PositionName})"
            });

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Thanh toán thành công."
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            // ============================================================
            // 1. KIỂM TRA NGƯỜI THỰC HIỆN
            // ============================================================
            var currentEmployee = await GetCurrentEmployee();

            if (!CanProcessPayment(currentEmployee))
            {
                TempData["Message"] =
                    "Bạn không có quyền hủy đơn hàng.";

                TempData["MessageType"] =
                    "error";

                return RedirectToAction(nameof(Index));
            }


            // ============================================================
            // 2. TÌM ORDER
            // ============================================================
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                TempData["Message"] =
                    "Không tìm thấy đơn hàng.";

                TempData["MessageType"] =
                    "error";

                return RedirectToAction(nameof(Index));
            }


            // ============================================================
            // 3. ĐƠN ĐÃ HỦY
            // ============================================================
            if (order.OrderStatus == "Cancelled" ||
                order.PaymentStatus == "Cancelled")
            {
                TempData["Message"] =
                    "Đơn hàng này đã được hủy trước đó.";

                TempData["MessageType"] =
                    "warning";

                return RedirectToAction(nameof(Index));
            }


            // ============================================================
            // 4. ĐƠN ĐÃ HOÀN THÀNH / ĐÃ THANH TOÁN
            //
            // Không được Cancel trực tiếp.
            // Sau này nếu cần sẽ làm chức năng Refund riêng.
            // ============================================================
            if (order.OrderStatus == "Completed" ||
                order.PaymentStatus == "Paid")
            {
                TempData["Message"] =
                    "Đơn hàng đã thanh toán hoặc hoàn thành, " +
                    "không thể hủy trực tiếp.";

                TempData["MessageType"] =
                    "error";

                return RedirectToAction(
                    nameof(Detail),
                    new { id = order.OrderId }
                );
            }


            // ============================================================
            // 5. CHỈ PENDING + UNPAID MỚI ĐƯỢC HỦY
            // ============================================================
            if (order.OrderStatus != "Pending" ||
                order.PaymentStatus != "Unpaid")
            {
                TempData["Message"] =
                    "Trạng thái hiện tại của đơn hàng không cho phép hủy.";

                TempData["MessageType"] =
                    "error";

                return RedirectToAction(
                    nameof(Detail),
                    new { id = order.OrderId }
                );
            }


            // ============================================================
            // 6. LƯU TRẠNG THÁI CŨ
            // ============================================================
            string oldStatus =
                order.OrderStatus;


            // ============================================================
            // 7. HỦY ORDER
            // ============================================================
            order.OrderStatus =
                "Cancelled";

            order.PaymentStatus =
                "Cancelled";


            // ============================================================
            // 8. NGƯỜI THỰC HIỆN
            // ============================================================
            string changedBy =
                currentEmployee != null
                    ? $"{currentEmployee.FullName} ({currentEmployee.Position?.PositionName})"
                    : User.Identity?.Name ?? "System";


            // ============================================================
            // 9. LOG
            // ============================================================
            _context.OrderStatusLogs.Add(
                new OrderStatusLog
                {
                    OrderId =
                        order.OrderId,

                    OldStatus =
                        oldStatus,

                    NewStatus =
                        "Cancelled",

                    ChangedAt =
                        DateTime.Now,

                    ChangedBy =
                        changedBy
                }
            );


            // ============================================================
            // 10. SAVE
            // ============================================================
            await _context.SaveChangesAsync();


            TempData["Message"] =
                $"Đã hủy đơn hàng #{order.OrderCode}.";

            TempData["MessageType"] =
                "success";


            return RedirectToAction(
                nameof(Index)
            );
        }
    }

}