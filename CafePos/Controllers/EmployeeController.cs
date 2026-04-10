using BCrypt.Net;
using CafePos.Data; // Thay bằng namespace DbContext của bạn
using CafePos.Models;
using CafePos.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CafePos.Controllers
{
    [Authorize] // Bắt buộc phải đăng nhập mới xem được hồ sơ
    public class EmployeeController : Controller
    {
        private readonly CafePosDbContext _db;
        private readonly PhotoService _employeeService;
        public EmployeeController(CafePosDbContext db, PhotoService employeeService) { _db = db; _employeeService = employeeService; }

        // 1. Danh sách nhân viên đang làm việc (IsActive = true)
        public async Task<IActionResult> Index()
        {
            var employees = await _db.Employees
                .Include(e => e.User)
                .Where(e => e.User.IsActive == true)
                .ToListAsync();
            return View(employees);
        }

        public IActionResult Create() => View();


        // 3. Chi tiết nhân viên                           
        public async Task<IActionResult> Detail(int? id)
        {
            // 1. Lấy UserId của người đang đăng nhập (Kết quả sẽ là 7 như trong ảnh)
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction("Login", "Account");
            int currentUserId = int.Parse(userIdClaim.Value);

            // 2. Nếu không truyền id, tự động hiểu là đang xem hồ sơ của chính mình
            if (id == null)
            {
                var myProfile = await _db.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(m => m.UserId == currentUserId);
                if (myProfile == null) return NotFound("Không tìm thấy hồ sơ của bạn.");
                return View(myProfile);
            }

            // 3. Nếu có truyền id, đi tìm nhân viên đó trong DB trước
            var targetEmployee = await _db.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.EmployeeId == id); // Tìm theo ID nhân viên trên URL

            if (targetEmployee == null) return NotFound("Không tìm thấy nhân viên này.");

            // 4. KIỂM TRA QUYỀN (Dòng quan trọng nhất):
            // Nếu UserId của nhân viên này khác với ID người đang đăng nhập -> CHẶN
            if (targetEmployee.UserId != currentUserId)
            {
                TempData["Message"] = "Bạn không có quyền xem hồ sơ của nhân viên khác!";
                TempData["MessageType"] = "warning";
                return RedirectToAction("Index", "Home");
            }

            // 5. Nếu trùng khớp (Chính là mình) -> Cho xem
            return View(targetEmployee);
        }

    }
}