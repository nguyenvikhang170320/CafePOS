using CafePos.Data;
using CafePos.Models;
using CafePos.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CafePos.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class EmployeeController : Controller
    {
        private readonly CafePosDbContext _db;
        private readonly PhotoService _employeeService;

        public EmployeeController(CafePosDbContext db, PhotoService employeeService)
        {
            _db = db;
            _employeeService = employeeService;
        }

        // ====================================================
        // 1. DANH SÁCH NHÂN VIÊN ĐANG LÀM VIỆC
        // ====================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var employees = await _db.Employees
                .Include(e => e.User)
                .Include(e => e.Position)
                .Where(e => e.IsActive && e.User != null && e.User.IsActive)
                .OrderBy(e => e.EmployeeId)
                .ToListAsync();

            return View(employees);
        }

        // ====================================================
        // 2. THÊM MỚI NHÂN VIÊN (TỰ ĐỘNG TẠO TÀI KHOẢN USER)
        // ====================================================
        [HttpGet]
        public IActionResult Create()
        {
            LoadPositions();
            return View(new EmployeeVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeVM obj, IFormFile? file)
        {
            // Validations thủ công cho Tài khoản
            if (string.IsNullOrWhiteSpace(obj.Username))
            {
                ModelState.AddModelError("Username", "Vui lòng nhập Tên đăng nhập.");
            }
            else if (await _db.Users.AnyAsync(u => u.Username == obj.Username.Trim()))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại trong hệ thống.");
            }

            if (string.IsNullOrWhiteSpace(obj.Password))
            {
                ModelState.AddModelError("Password", "Vui lòng nhập Mật khẩu khởi tạo.");
            }

            if (!string.IsNullOrWhiteSpace(obj.Email) && await _db.Users.AnyAsync(u => u.Email == obj.Email.Trim()))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
            }

            if (!obj.PositionId.HasValue)
            {
                ModelState.AddModelError("PositionId", "Vui lòng chọn Chức danh cho nhân viên.");
            }

            // Xóa validation không dùng vì UserId sẽ tự sinh
            ModelState.Remove("UserId");

            if (ModelState.IsValid)
            {
                using var transaction = await _db.Database.BeginTransactionAsync();
                try
                {
                    // 1. Xử lý ảnh đại diện (nếu có)
                    if (file != null && file.Length > 0)
                    {
                        var result = await _employeeService.AddPhotoAsync(file, "CafePos/Employee");
                        obj.ImageUrl = result.SecureUrl.ToString();
                    }
                    else if (!string.IsNullOrWhiteSpace(obj.ImageUrl))
                    {
                        obj.ImageUrl = obj.ImageUrl.Trim();
                    }

                    // 2. Tạo Tài khoản User (Ép RoleId = 3 cho Nhân viên)
                    var newUser = new User
                    {
                        Username = obj.Username.Trim(),
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(obj.Password),
                        Email = string.IsNullOrWhiteSpace(obj.Email) ? null : obj.Email.Trim(),
                        RoleId = 3, // Role 3 = Nhân viên
                        IsActive = true,
                        TrangThai = "Hoạt động",
                        NgayCapNhat = DateTime.Now
                    };

                    _db.Users.Add(newUser);
                    await _db.SaveChangesAsync(); // Lưu để lấy UserId tự tăng

                    // 3. Tạo Hồ sơ Employee gắn với UserId vừa sinh
                    var newEmp = new Employee
                    {
                        UserId = newUser.UserId,
                        FullName = obj.FullName.Trim(),
                        PositionId = obj.PositionId,
                        EmployeeCode = string.IsNullOrWhiteSpace(obj.EmployeeCode)
                                        ? $"NV{newUser.UserId:000}"
                                        : obj.EmployeeCode.Trim(),
                        PhoneNumber = obj.PhoneNumber?.Trim(),
                        Address = obj.Address?.Trim(),
                        ImageUrl = obj.ImageUrl,
                        HireDate = DateTime.Now,
                        IsActive = true
                    };

                    _db.Employees.Add(newEmp);
                    await _db.SaveChangesAsync();

                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Thêm nhân viên '{newEmp.FullName}' và tạo tài khoản thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Có lỗi xảy ra khi lưu dữ liệu: " + ex.Message);
                }
            }

            LoadPositions(obj.PositionId);
            return View(obj);
        }

        // ====================================================
        // 3. CẬP NHẬT THÔNG TIN NHÂN VIÊN
        // ====================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var emp = await _db.Employees
                .Include(e => e.User)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(x => x.EmployeeId == id);

            if (emp == null) return NotFound();

            var vm = new EmployeeVM
            {
                UserId = emp.UserId,
                FullName = emp.FullName,
                PositionId = emp.PositionId,
                Username = emp.User?.Username,
                Email = emp.User?.Email,
                EmployeeCode = emp.EmployeeCode,
                PhoneNumber = emp.PhoneNumber,
                Address = emp.Address,
                ImageUrl = emp.ImageUrl
            };

            LoadPositions(emp.PositionId);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmployeeVM obj, IFormFile? file)
        {
            var empDb = await _db.Employees
                 .Include(e => e.User)
                 .FirstOrDefaultAsync(x => x.EmployeeId == id);

            if (empDb == null) return NotFound();

            // Loại bỏ validation không cần thiết khi sửa
            ModelState.Remove("UserId");
            ModelState.Remove("Username");
            ModelState.Remove("Password");

            if (!obj.PositionId.HasValue)
            {
                ModelState.AddModelError("PositionId", "Vui lòng chọn chức danh cho nhân viên.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Cập nhật bảng Employee
                    empDb.FullName = obj.FullName.Trim();
                    empDb.PositionId = obj.PositionId;
                    empDb.EmployeeCode = obj.EmployeeCode?.Trim();
                    empDb.PhoneNumber = obj.PhoneNumber?.Trim();
                    empDb.Address = obj.Address?.Trim();

                    // Cập nhật Email bên bảng User nếu có thay đổi
                    if (empDb.User != null && empDb.User.Email != obj.Email)
                    {
                        empDb.User.Email = string.IsNullOrWhiteSpace(obj.Email) ? null : obj.Email.Trim();
                        empDb.User.NgayCapNhat = DateTime.Now;
                    }

                    // Cập nhật ảnh nếu chọn file mới
                    if (file != null && file.Length > 0)
                    {
                        var result = await _employeeService.AddPhotoAsync(file, "CafePos/Employee");
                        empDb.ImageUrl = result.SecureUrl.ToString();
                    }

                    await _db.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật thông tin nhân viên thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi khi cập nhật: " + ex.Message);
                }
            }

            obj.Username = empDb.User?.Username;
            LoadPositions(obj.PositionId);
            return View(obj);
        }

        // ====================================================
        // 4. XÓA MỀM (KHÓA TÀI KHOẢN + CHUYỂN VÀO THÙNG RÁC)
        // ====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var emp = await _db.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (emp == null) return NotFound();

            // Vô hiệu hóa ở cả 2 bảng
            emp.IsActive = false;
            if (emp.User != null)
            {
                emp.User.IsActive = false;
                emp.User.TrangThai = "Khóa";
                emp.User.NgayCapNhat = DateTime.Now;
            }

            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã khóa và chuyển nhân viên '{emp.FullName}' vào thùng rác!";

            return RedirectToAction(nameof(Index));
        }

        // ====================================================
        // 5. THÙNG RÁC & KHÔI PHỤC
        // ====================================================
        [HttpGet]
        public async Task<IActionResult> Trash()
        {
            var trashList = await _db.Employees
                .Include(e => e.User)
                .Include(e => e.Position)
                .Where(e => !e.IsActive || (e.User != null && !e.User.IsActive))
                .OrderBy(e => e.EmployeeId)
                .ToListAsync();

            return View(trashList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var emp = await _db.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (emp == null) return NotFound();

            // Khôi phục cả 2 bảng
            emp.IsActive = true;
            if (emp.User != null)
            {
                emp.User.IsActive = true;
                emp.User.TrangThai = "Hoạt động";
                emp.User.NgayCapNhat = DateTime.Now;
            }

            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Khôi phục nhân viên '{emp.FullName}' thành công!";

            return RedirectToAction(nameof(Trash));
        }

        // ====================================================
        // 6. CHI TIẾT NHÂN VIÊN
        // ====================================================
        [HttpGet]
        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _db.Employees
                .Include(e => e.User)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null) return NotFound();

            return View(employee);
        }

        // ====================================================
        // HELPER METHODS
        // ====================================================
        private void LoadPositions(int? selectedPositionId = null)
        {
            var positions = _db.Positions
                .Where(p => p.IsActive)
                .OrderBy(p => p.PositionName)
                .ToList();

            ViewBag.PositionId = new SelectList(positions, "PositionId", "PositionName", selectedPositionId);
        }
    }
}