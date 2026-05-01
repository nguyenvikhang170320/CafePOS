using CafePos.Data;
using CafePos.Models;
using CafePos.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKTX.Services;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CafePos.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly CafePosDbContext _context;
        public AccountController(IAuthService authService, CafePosDbContext context)
        {
            _authService = authService;
            _context = context;
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _authService.LoginAsync(username, password);

            if (user != null)
            {
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username ?? ""),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(ClaimTypes.Role, user.Role?.Name ?? ""),
            new Claim("FullName", user.FullName ?? "Người dùng"),
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
        };

                var claimsIdentity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string password, string fullName, string email)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email))
            {
                ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }

            var existedUser = await _context.Users.AnyAsync(x => x.Username == username);
            if (existedUser)
            {
                ViewBag.ErrorMessage = "Tên đăng nhập đã tồn tại.";
                return View();
            }

            var existedEmail = await _context.Users.AnyAsync(x => x.Email == email);
            if (existedEmail)
            {
                ViewBag.ErrorMessage = "Email đã tồn tại.";
                return View();
            }

            var newUser = new User
            {
                Username = username,
                FullName = fullName,
                Email = email,
                RoleId = 2,
                IsActive = true,
                TrangThai = "Hoạt động",
                NgayCapNhat = DateTime.Now
            };

            await _authService.RegisterAsync(newUser, password);

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

        // ========== QUÊN MẬT KHẨU ==========
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "Email không tồn tại trong hệ thống.";
                return View();
            }

            // Sinh token
            var token = Guid.NewGuid().ToString();

            // Lưu token
            var resetToken = new PasswordResetToken
            {
                UserId = user.UserId,
                Token = token,
                ExpiredAt = DateTime.Now.AddMinutes(30)
            };
            _context.PasswordResetToken.Add(resetToken);
            await _context.SaveChangesAsync();

            // Tạo link reset
            var resetLink = Url.Action("ResetPassword", "Account", new { token = token }, Request.Scheme);

            // Gửi mail
            await EmailService.SendEmailAsync(user.Email, "Khôi phục mật khẩu QLKTX",
                $"<p>Xin chào {user.Username},</p>" +
                $"<p>Bạn đã yêu cầu đặt lại mật khẩu. Nhấn vào liên kết bên dưới để đặt lại:</p>" +
                $"<p><a href='{resetLink}'>Đặt lại mật khẩu</a></p>" +
                $"<p>Liên kết có hiệu lực trong 30 phút.</p>");

            ViewBag.Message = "Đã gửi liên kết đặt lại mật khẩu tới email của bạn.";
            return View();
        }

        // ========== ĐẶT LẠI MẬT KHẨU ==========
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            var resetToken = await _context.PasswordResetToken
                .FirstOrDefaultAsync(t => t.Token == token && t.ExpiredAt > DateTime.Now);

            if (resetToken == null)
            {
                ViewBag.Error = "Liên kết không hợp lệ hoặc đã hết hạn.";
                return View("InvalidToken");
            }

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string token, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                ViewBag.Token = token;
                return View();
            }

            var resetToken = await _context.PasswordResetToken
                .FirstOrDefaultAsync(t => t.Token == token && t.ExpiredAt > DateTime.Now);

            if (resetToken == null)
            {
                ViewBag.Error = "Token không hợp lệ hoặc đã hết hạn.";
                return View("InvalidToken");
            }

            var user = await _context.Users.FindAsync(resetToken.UserId);
            if (user == null)
            {
                ViewBag.Error = "Người dùng không tồn tại.";
                return View("InvalidToken");
            }

            // Cập nhật mật khẩu (hash nếu có)
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword); // bạn có thể dùng BCrypt.Net nếu muốn mã hóa
            _context.PasswordResetToken.Remove(resetToken);
            await _context.SaveChangesAsync();

            ViewBag.Message = "Đặt lại mật khẩu thành công! Hãy đăng nhập lại.";
            return RedirectToAction("Login");
        }
    }
}