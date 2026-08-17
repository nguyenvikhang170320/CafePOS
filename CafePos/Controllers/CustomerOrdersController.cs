using CafePos.Data;
using CafePos.Models;
using CafePos.Models.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CafePos.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerOrderController : Controller
    {
        private readonly CafePosDbContext _context;
        private readonly BankingSettings _bankingSettings;

        public CustomerOrderController(
            CafePosDbContext context,
            IOptions<BankingSettings> bankingOptions)
        {
            _context = context;
            _bankingSettings = bankingOptions.Value;
        }


        // =========================================================
        // GET: CustomerOrder/Create
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCreateViewData();

            return View();
        }


        // =========================================================
        // POST: CustomerOrder/Create
        // Khách đặt món giao online
        // Chỉ hỗ trợ Cash hoặc Banking
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string? note,
            string? phone,
            string? paymentMethod,
            List<int> productIds,
            List<int> quantities,
            List<int?>? productSizeIds)
        {
            await LoadCreateViewData();


            var customerName =
                User.FindFirst("FullName")?.Value
                ?? User.Identity?.Name
                ?? "Khách hàng";


            var userIdClaim =
                User.FindFirst(
                    ClaimTypes.NameIdentifier
                )?.Value;


            // =====================================================
            // KIỂM TRA SỐ ĐIỆN THOẠI
            // =====================================================
            if (string.IsNullOrWhiteSpace(phone))
            {
                TempData["Message"] =
                    "Vui lòng nhập số điện thoại";

                TempData["MessageType"] =
                    "error";

                return View();
            }


            // =====================================================
            // KIỂM TRA USER
            // =====================================================
            if (
                string.IsNullOrWhiteSpace(userIdClaim)
                ||
                !int.TryParse(
                    userIdClaim,
                    out int userId
                )
            )
            {
                TempData["Message"] =
                    "Không xác định được tài khoản đăng nhập";

                TempData["MessageType"] =
                    "error";

                return View();
            }


            // =====================================================
            // CHỈ CHO CUSTOMER CHỌN CASH HOẶC BANKING
            // =====================================================
            paymentMethod =
                string.IsNullOrWhiteSpace(paymentMethod)
                    ? "Cash"
                    : paymentMethod.Trim();


            var validPaymentMethods =
                new[]
                {
                    "Cash",
                    "Banking"
                };


            if (
                !validPaymentMethods.Contains(
                    paymentMethod,
                    StringComparer.OrdinalIgnoreCase
                )
            )
            {
                TempData["Message"] =
                    "Phương thức thanh toán không hợp lệ";

                TempData["MessageType"] =
                    "error";

                return View();
            }


            // Chuẩn hóa đúng chuỗi lưu DB
            paymentMethod =
                paymentMethod.Equals(
                    "Banking",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? "Banking"
                    : "Cash";


            // =====================================================
            // KIỂM TRA DANH SÁCH MÓN
            // =====================================================
            if (
                productIds == null
                ||
                quantities == null
                ||
                !productIds.Any()
                ||
                !quantities.Any()
            )
            {
                TempData["Message"] =
                    "Dữ liệu món chọn không hợp lệ";

                TempData["MessageType"] =
                    "error";

                return View();
            }


            // =====================================================
            // LOAD TOPPING
            // =====================================================
            var allToppings =
                await _context.Toppings
                    .Where(x => x.IsActive)
                    .ToListAsync();


            var validItems =
                new List<OrderItem>();


            decimal subTotal = 0;


            // =====================================================
            // TẠO DANH SÁCH ORDER ITEM
            // =====================================================
            for (
                int i = 0;
                i < productIds.Count;
                i++
            )
            {
                if (i >= quantities.Count)
                {
                    continue;
                }


                int quantity =
                    quantities[i];


                if (quantity <= 0)
                {
                    continue;
                }


                var product =
                    await _context.Products
                        .FirstOrDefaultAsync(
                            x =>
                                x.ProductId ==
                                productIds[i]
                                &&
                                x.IsActive
                        );


                if (product == null)
                {
                    continue;
                }


                decimal extraPrice = 0;

                string? sizeName = null;

                int? selectedSizeId = null;


                // =================================================
                // SIZE
                // =================================================
                if (
                    productSizeIds != null
                    &&
                    i < productSizeIds.Count
                )
                {
                    selectedSizeId =
                        productSizeIds[i];


                    if (
                        selectedSizeId.HasValue
                        &&
                        selectedSizeId.Value > 0
                    )
                    {
                        var productSize =
                            await _context.ProductSizes
                                .FirstOrDefaultAsync(
                                    ps =>
                                        ps.ProductSizeId ==
                                        selectedSizeId.Value
                                        &&
                                        ps.ProductId ==
                                        product.ProductId
                                        &&
                                        !ps.IsDeleted
                                );


                        if (productSize != null)
                        {
                            extraPrice =
                                productSize.ExtraPrice;

                            sizeName =
                                productSize.SizeName;
                        }
                    }
                }


                var orderItem =
                    new OrderItem
                    {
                        ProductId =
                            product.ProductId,

                        Quantity =
                            quantity,

                        ProductNameSnapshot =
                            product.Name,

                        UnitPrice =
                            product.BasePrice
                            +
                            extraPrice,

                        ProductSizeId =
                            selectedSizeId,

                        SizeNameSnapshot =
                            sizeName,

                        ItemNote =
                            null,

                        OrderItemToppings =
                            new List<OrderItemTopping>()
                    };


                // =================================================
                // TOPPING
                // =================================================
                var toppingFormValues =
                    Request.Form[
                        "itemToppings_" + i
                    ];


                decimal toppingTotal = 0;


                if (
                    !string.IsNullOrEmpty(
                        toppingFormValues
                    )
                )
                {
                    var selectedToppingIds =
                        toppingFormValues
                            .ToString()
                            .Split(
                                ',',
                                StringSplitOptions
                                    .RemoveEmptyEntries
                            )
                            .Select(int.Parse)
                            .ToList();


                    foreach (
                        var topId
                        in selectedToppingIds
                    )
                    {
                        var top =
                            allToppings
                                .FirstOrDefault(
                                    t =>
                                        t.ToppingId
                                        ==
                                        topId
                                );


                        if (top == null)
                        {
                            continue;
                        }


                        var itemTopping =
                            new OrderItemTopping
                            {
                                ToppingId =
                                    top.ToppingId,

                                ToppingNameSnapshot =
                                    top.Name,

                                Price =
                                    top.Price,

                                Quantity =
                                    quantity,

                                TotalPrice =
                                    top.Price
                                    *
                                    quantity
                            };


                        orderItem
                            .OrderItemToppings
                            .Add(
                                itemTopping
                            );


                        toppingTotal +=
                            itemTopping.TotalPrice;
                    }
                }


                orderItem.LineTotal =
                    (
                        orderItem.UnitPrice
                        *
                        quantity
                    )
                    +
                    toppingTotal;


                subTotal +=
                    orderItem.LineTotal;


                validItems.Add(
                    orderItem
                );
            }


            // =====================================================
            // PHẢI CÓ ÍT NHẤT 1 MÓN
            // =====================================================
            if (!validItems.Any())
            {
                TempData["Message"] =
                    "Vui lòng chọn ít nhất 1 món và nhập số lượng lớn hơn 0";

                TempData["MessageType"] =
                    "error";

                return View();
            }


            // =====================================================
            // TẠO ORDER ONLINE
            // =====================================================
            var order =
                new Order
                {
                    OrderCode = $"CPOS-{DateTime.Now.ToString(
                            "yyyyMMdd-HHmmss"
                        )}",
                   
                    CustomerName =
                        customerName,

                    CustomerPhone =
                        phone,

                    UserId =
                        userId,

                    CreatedDate =
                        DateTime.Now,

                    Note =
                        note,

                    // Chưa xử lý/giao xong
                    OrderStatus =
                        "Pending",

                    // Cash hay Banking lúc mới đặt
                    // đều chưa được quán xác nhận tiền
                    PaymentStatus =
                        "Unpaid",

                    PaymentMethod =
                        paymentMethod,

                    SubTotal =
                        subTotal,

                    DiscountAmount =
                        0,

                    TotalAmount =
                        subTotal,

                    OrderItems =
                        validItems
                };


            _context.Orders.Add(
                order
            );


            await _context
                .SaveChangesAsync();


            // =====================================================
            // THÔNG BÁO
            // =====================================================
            if (
                paymentMethod ==
                "Banking"
            )
            {
                TempData["Message"] =
                    "Đặt món thành công. Vui lòng quét QR để chuyển khoản.";

                TempData["MessageType"] =
                    "success";


                // Banking:
                // chuyển thẳng sang Detail để hiện VietQR
                return RedirectToAction(
                    nameof(Detail),
                    new
                    {
                        id =
                            order.OrderId
                    }
                );
            }


            // CASH:
            // khách sẽ trả tiền khi nhận hàng
            TempData["Message"] =
                "Đặt món thành công. Bạn sẽ thanh toán tiền mặt khi nhận hàng.";

            TempData["MessageType"] =
                "success";


            return RedirectToAction(
                nameof(MyOrders)
            );
        }


        // =========================================================
        // LOAD DATA CHO CREATE
        // =========================================================
        private async Task LoadCreateViewData()
        {
            ViewBag.Products =
                await _context.Products
                    .Include(
                        p =>
                            p.ProductSizes
                                .Where(
                                    ps =>
                                        !ps.IsDeleted
                                )
                    )
                    .Where(
                        x =>
                            x.IsActive
                    )
                    .OrderBy(
                        x =>
                            x.Name
                    )
                    .ToListAsync();


            ViewBag.Toppings =
                await _context.Toppings
                    .Where(
                        x =>
                            x.IsActive
                    )
                    .OrderBy(
                        x =>
                            x.Name
                    )
                    .ToListAsync();


            ViewBag.CustomerName =
                User.FindFirst(
                    "FullName"
                )?.Value
                ??
                User.Identity?.Name
                ??
                "";


            ViewBag.CustomerPhone =
                User.FindFirst(
                    ClaimTypes.MobilePhone
                )?.Value

                ??
                User.FindFirst(
                    "PhoneNumber"
                )?.Value

                ??
                "";
        }


        // =========================================================
        // GÁN THÔNG TIN BANKING CHO VIEW
        // =========================================================
        private void LoadBankingViewData()
        {
            ViewBag.BankId =
                _bankingSettings.BankId;

            ViewBag.BankAccountNumber =
                _bankingSettings.AccountNumber;

            ViewBag.BankAccountName =
                _bankingSettings.AccountName;
        }


        // =========================================================
        // ĐƠN HÀNG CỦA TÔI
        // =========================================================
        public async Task<IActionResult> MyOrders()
        {
            var userIdClaim =
                User.FindFirst(
                    ClaimTypes.NameIdentifier
                )?.Value;


            if (
                string.IsNullOrEmpty(
                    userIdClaim
                )
                ||
                !int.TryParse(
                    userIdClaim,
                    out int userId
                )
            )
            {
                TempData["Message"] =
                    "Không xác định được tài khoản đăng nhập";

                TempData["MessageType"] =
                    "error";

                return View(
                    new List<Order>()
                );
            }


            var orders =
                await _context.Orders
                    .Where(
                        x =>
                            x.UserId
                            ==
                            userId
                    )
                    .OrderByDescending(
                        x =>
                            x.CreatedDate
                    )
                    .ToListAsync();


            return View(
                orders
            );
        }


        // =========================================================
        // HÓA ĐƠN CỦA CUSTOMER
        // =========================================================
        public async Task<IActionResult> Invoice(
            string? phone)
        {
            var userIdClaim =
                User.FindFirst(
                    ClaimTypes.NameIdentifier
                )?.Value;


            int.TryParse(
                userIdClaim,
                out int userId
            );


            var orders =
                await _context.Orders
                    .Where(
                        x =>
                            x.UserId
                            ==
                            userId
                    )
                    .OrderByDescending(
                        x =>
                            x.CreatedDate
                    )
                    .ToListAsync();


            ViewBag.Phone =
                phone;


            return View(
                orders
            );
        }


        // =========================================================
        // AJAX PARTIAL HÓA ĐƠN
        // Banking cần thông tin VietQR
        // =========================================================
        [HttpGet]
        public async Task<IActionResult>
            GetInvoicePartial(int id)
        {
            var userIdClaim =
                User.FindFirst(
                    ClaimTypes.NameIdentifier
                )?.Value;


            int.TryParse(
                userIdClaim,
                out int userId
            );


            var order =
                await _context.Orders

                    .Include(
                        x =>
                            x.OrderItems
                    )

                    .ThenInclude(
                        x =>
                            x.OrderItemToppings
                    )

                    .FirstOrDefaultAsync(
                        x =>
                            x.OrderId == id
                            &&
                            x.UserId == userId
                    );


            if (order == null)
            {
                return NotFound();
            }


            // Banking / VietQR
            LoadBankingViewData();


            return PartialView(
                "_CustomerInvoicePartial",
                order
            );
        }


        // =========================================================
        // CHI TIẾT ĐƠN CUSTOMER
        // Banking cũng hiện VietQR tại đây
        // =========================================================
        public async Task<IActionResult> Detail(
            int id)
        {
            var userIdClaim =
                User.FindFirst(
                    ClaimTypes.NameIdentifier
                )?.Value;


            int.TryParse(
                userIdClaim,
                out int userId
            );


            var order =
                await _context.Orders

                    .Include(
                        x =>
                            x.OrderItems
                    )

                    .ThenInclude(
                        x =>
                            x.OrderItemToppings
                    )

                    .FirstOrDefaultAsync(
                        x =>
                            x.OrderId == id
                            &&
                            x.UserId == userId
                    );


            if (order == null)
            {
                return NotFound();
            }


            // Banking / VietQR
            LoadBankingViewData();


            return View(
                order
            );
        }
    }
}