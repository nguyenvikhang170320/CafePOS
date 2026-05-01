using CafePos.Data;
using CafePos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;
using System.Security.Claims;

namespace CafePos.Controllers
{
    [Authorize]
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