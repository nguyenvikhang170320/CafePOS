using CafePos.Data;
using CafePos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;
using System.Security.Claims;

namespace CafePos.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TaiKhoanController : Controller
    {
        private readonly CafePosDbContext _context;

        public TaiKhoanController(CafePosDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claim, out int id)) return id;
            return null;
        }

        private void LoadRoles(int? selectedRoleId = null)
        {
            var roles = _context.Roles
                .Where(r => r.RoleId != 1)
                .OrderBy(r => r.RoleId)
                .ToList();

            ViewBag.RoleId = new SelectList(roles, "RoleId", "Name", selectedRoleId);
        }

        private void LoadTrangThai(string? selected = null)
        {
            ViewBag.TrangThais = new SelectList(new List<string> { "Hoạt động", "Khóa" }, selected);
        }

        public IActionResult Index(int? page, string searchString, bool showDeleted = false)
        {
            bool isAdmin = User.IsInRole("Admin");
            int? currentUserId = GetCurrentUserId();

            var query = _context.Users
                .Include(x => x.Role)
                .AsQueryable();

            if (showDeleted)
            {
                if (!isAdmin) return Forbid();
                query = query.Where(x => x.IsActive == false);
            }
            else
            {
                query = query.Where(x => x.IsActive == true);
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(x => x.Username.Contains(searchString)
                    || x.FullName.Contains(searchString)
                    || (x.Email != null && x.Email.Contains(searchString)));
            }

            if (!isAdmin && currentUserId.HasValue)
            {
                query = query.Where(x => x.UserId == currentUserId.Value);
            }

            query = query.OrderBy(x => x.UserId);

            ViewData["CurrentFilter"] = searchString;
            ViewBag.ShowDeleted = showDeleted;

            return View(query.ToPagedList(page ?? 1, 10));
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.UserId == id);

            if (user == null) return NotFound();
            return View(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            LoadRoles();
            LoadTrangThai("Hoạt động");

            var model = new User
            {
                TrangThai = "Hoạt động"
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(User users)
        {
            if (users.RoleId == 1)
            {
                ModelState.AddModelError("RoleId", "Không được phép tạo tài khoản Admin!");
            }

            if (_context.Users.Any(x => x.Username == users.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại!");
            }

            if (!string.IsNullOrWhiteSpace(users.Email) &&
                _context.Users.Any(x => x.Email == users.Email))
            {
                ModelState.AddModelError("Email", "Email đã tồn tại!");
            }

            if (string.IsNullOrWhiteSpace(users.PasswordHash))
            {
                ModelState.AddModelError("PasswordHash", "Mật khẩu không được để trống!");
            }

            if (ModelState.IsValid)
            {
                users.PasswordHash = BCrypt.Net.BCrypt.HashPassword(users.PasswordHash);
                users.IsActive = true;
                users.TrangThai = string.IsNullOrWhiteSpace(users.TrangThai) ? "Hoạt động" : users.TrangThai;
                users.NgayCapNhat = DateTime.Now;

                _context.Users.Add(users);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Tạo tài khoản thành công!";
                return RedirectToAction(nameof(Index));
            }

            users.PasswordHash = string.Empty;
            LoadRoles(users.RoleId == 0 ? null : users.RoleId);
            LoadTrangThai(users.TrangThai);
            return View(users);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == id);

            if (user == null) return NotFound();

            LoadTrangThai(user.TrangThai);
            return View(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User model)
        {
            if (id != model.UserId) return NotFound();

            var existing = await _context.Users
                .FirstOrDefaultAsync(x => x.UserId == id);

            if (existing == null) return NotFound();

            if (_context.Users.Any(x => x.Username == model.Username && x.UserId != model.UserId))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại!");
            }

            if (!string.IsNullOrWhiteSpace(model.Email) &&
                _context.Users.Any(x => x.Email == model.Email && x.UserId != model.UserId))
            {
                ModelState.AddModelError("Email", "Email đã tồn tại!");
            }

            ModelState.Remove("PasswordHash");
            ModelState.Remove("Role");
            ModelState.Remove("RoleId");

            if (ModelState.IsValid)
            {
                existing.Username = model.Username;
                existing.FullName = model.FullName;
                existing.Email = model.Email;
                existing.TrangThai = model.TrangThai;
                existing.IsActive = model.TrangThai == "Hoạt động";
                existing.NgayCapNhat = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Cập nhật tài khoản '{existing.Username}' thành công!";
                return RedirectToAction(nameof(Index));
            }

            LoadTrangThai(model.TrangThai);
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = false;
                user.TrangThai = "Khóa";
                user.NgayCapNhat = DateTime.Now;

                _context.Update(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã xóa tài khoản '{user.Username}' thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Restore(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = true;
                user.TrangThai = "Hoạt động";
                user.NgayCapNhat = DateTime.Now;

                _context.Update(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Khôi phục tài khoản '{user.Username}' thành công!";
            }

            return RedirectToAction(nameof(Index), new { showDeleted = true });
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            bool isAdmin = User.IsInRole("Admin");
            int? currentUserId = GetCurrentUserId();

            if (!isAdmin && (!currentUserId.HasValue || user.UserId != currentUserId.Value))
            {
                TempData["ErrorMessage"] = "Bạn chỉ có quyền đổi mật khẩu của chính mình.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Username = user.Username;
            ViewBag.UserId = user.UserId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(int id, string oldPassword, string newPassword, string confirmPassword)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            bool isAdmin = User.IsInRole("Admin");
            int? currentUserId = GetCurrentUserId();

            if (!isAdmin && (!currentUserId.HasValue || user.UserId != currentUserId.Value))
            {
                TempData["ErrorMessage"] = "Bạn chỉ có quyền đổi mật khẩu của chính mình.";
                return RedirectToAction(nameof(Index));
            }

            bool isHashed =
                user.PasswordHash.StartsWith("$2a$") ||
                user.PasswordHash.StartsWith("$2b$") ||
                user.PasswordHash.StartsWith("$2y$");

            bool isValidOld = isHashed
                ? BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash)
                : user.PasswordHash == oldPassword;

            if (!isValidOld)
            {
                ViewBag.Error = "Mật khẩu cũ không đúng.";
                ViewBag.Username = user.Username;
                ViewBag.UserId = user.UserId;
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                ViewBag.Username = user.Username;
                ViewBag.UserId = user.UserId;
                return View();
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.NgayCapNhat = DateTime.Now;

            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue) return RedirectToAction("Index", "Home");

            var user = await _context.Users
                .Include(x => x.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == currentUserId.Value);

            if (user == null) return NotFound();

            return View(user);
        }
    }
}