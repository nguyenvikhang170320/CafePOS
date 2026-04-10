using CafePos.Models;
using CafePos.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CafePos.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        // ==========================================
        // 1. ĐĂNG NHẬP
        // ==========================================
        [HttpGet]
        public IActionResult Login()
        {
            // Nếu người dùng đã đăng nhập rồi mà lỡ bấm vào trang Login thì tự đẩy về trang chủ
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _authService.LoginAsync(username, password);

            if (user != null)
            {
                // Tạo "Thẻ căn cước" (Claims) cho người dùng
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role?   .Name ?? ""), // Lấy phân quyền từ DB
                    new Claim("FullName", user.FullName ?? "Người dùng"),

                    // --- DÒNG QUAN TRỌNG NHẤT NẰM Ở ĐÂY ---
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())

                };

                // Đóng dấu căn cước vào hệ thống Cookie
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true // Nhớ đăng nhập kể cả khi tắt trình duyệt (tùy chọn)
                };

                // Chính thức đăng nhập
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);
                
                // Kiểm tra nếu là Admin thì đá vào Area Admin
                if (user.Role?.Name == "Admin")
                {
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }

                return RedirectToAction("Index", "Home");
            }

            ViewBag.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác!";
            return View();
        }

        // ==========================================
        // 2. ĐĂNG KÝ
        // ==========================================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string username, string password, string fullName)
        {
            var newUser = new User
            {
                Username = username,
                FullName = fullName,
                // Mặc định tạo tài khoản mới sẽ gán cho chức vụ Staff (RoleId = 2)
                RoleId = 2
            };

            await _authService.RegisterAsync(newUser, password);

            // Dùng TempData để hiển thị thông báo bên trang Login sau khi chuyển hướng
            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        // ==========================================
        // 3. ĐĂNG XUẤT
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            // Xóa Cookie và đăng xuất
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ==========================================
        // 4. QUÊN MẬT KHẨU (Khung chờ sẵn)
        // ==========================================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // ==========================================
        // 5. ĐỔI MẬT KHẨU (Khung chờ sẵn)
        // ==========================================
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }
    }
}