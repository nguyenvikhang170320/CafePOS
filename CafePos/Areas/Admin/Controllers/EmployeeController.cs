using CafePos.Data;
using CafePos.Models;
using CafePos.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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

        // 1. Danh sách nhân viên đang làm việc
        public async Task<IActionResult> Index()
        {
            var employees = await _db.Employees
                .Include(e => e.User)
                .Where(e => e.User != null && e.User.IsActive)
                .OrderBy(e => e.EmployeeId)
                .ToListAsync();

            return View(employees);
        }

        // 2. Thêm nhân viên - GET
        [HttpGet]
        public IActionResult Create()
        {
            LoadAvailableUsers();
            return View(new EmployeeVM());
        }

        // 3. Thêm nhân viên - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeVM obj, IFormFile? file)
        {
            if (!obj.UserId.HasValue)
            {
                ModelState.AddModelError("UserId", "Vui lòng chọn tài khoản nhân viên.");
            }

            User? user = null;

            if (obj.UserId.HasValue)
            {
                user = await _db.Users
                    .FirstOrDefaultAsync(u => u.UserId == obj.UserId.Value && u.IsActive && u.RoleId == 3);

                if (user == null)
                {
                    ModelState.AddModelError("UserId", "Tài khoản nhân viên không hợp lệ.");
                }
                else
                {
                    bool existedEmployee = await _db.Employees
                        .AnyAsync(e => e.UserId == obj.UserId.Value);

                    if (existedEmployee)
                    {
                        ModelState.AddModelError("UserId", "Tài khoản này đã có hồ sơ nhân viên rồi.");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (file != null && file.Length > 0)
                    {
                        var result = await _employeeService.AddPhotoAsync(file, "CafePos/Employee");
                        obj.ImageUrl = result.SecureUrl.ToString();
                    }
                    else if (!string.IsNullOrWhiteSpace(obj.ImageUrl))
                    {
                        obj.ImageUrl = obj.ImageUrl.Trim();
                    }
                    else
                    {
                        obj.ImageUrl = null;
                    }

                    var newEmp = new Employee
                    {
                        UserId = obj.UserId!.Value,
                        EmployeeCode = string.IsNullOrWhiteSpace(obj.EmployeeCode)
                            ? "NV" + obj.UserId.Value
                            : obj.EmployeeCode,
                        PhoneNumber = obj.PhoneNumber,
                        Address = obj.Address,
                        ImageUrl = obj.ImageUrl,
                        HireDate = DateTime.Now
                    };

                    _db.Employees.Add(newEmp);
                    await _db.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Thêm nhân viên thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi khi lưu nhân viên: " + ex.Message);
                }
            }

            LoadAvailableUsers(obj.UserId);
            return View(obj);
        }

        // 4. Thùng rác
        public async Task<IActionResult> Trash()
        {
            var trashList = await _db.Employees
                .Include(e => e.User)
                .Where(e => e.User != null && !e.User.IsActive)
                .OrderBy(e => e.EmployeeId)
                .ToListAsync();

            return View(trashList);
        }

        // 5. Chi tiết nhân viên
        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _db.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null) return NotFound();

            return View(employee);
        }

        // 6. Chỉnh sửa - GET
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var emp = await _db.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(x => x.EmployeeId == id);

            if (emp == null) return NotFound();

            var vm = new EmployeeVM
            {
                UserId = emp.UserId,
                FullName = emp.User?.FullName,
                Username = emp.User?.Username,
                EmployeeCode = emp.EmployeeCode,
                PhoneNumber = emp.PhoneNumber,
                Address = emp.Address,
                ImageUrl = emp.ImageUrl
            };

            return View(vm);
        }

        // 7. Chỉnh sửa - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmployeeVM obj, IFormFile? file)
        {
            var empDb = await _db.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(x => x.EmployeeId == id);

            if (empDb == null) return NotFound();

            ModelState.Remove("UserId");
            ModelState.Remove("Username");
            ModelState.Remove("Password");

            if (ModelState.IsValid)
            {
                try
                {
                    if (empDb.User != null)
                    {
                        empDb.User.FullName = obj.FullName;
                    }

                    empDb.EmployeeCode = obj.EmployeeCode;
                    empDb.PhoneNumber = obj.PhoneNumber;
                    empDb.Address = obj.Address;

                    if (file != null)
                    {
                        var result = await _employeeService.AddPhotoAsync(file, "CafePos/Employee");
                        empDb.ImageUrl = result.SecureUrl.ToString();
                    }

                    await _db.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật nhân viên thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi khi cập nhật nhân viên: " + ex.Message);
                }
            }

            obj.Username = empDb.User?.Username;
            return View(obj);
        }

        // 8. Xóa tạm thời
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var emp = await _db.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (emp == null) return NotFound();

            if (emp.User != null)
            {
                emp.User.IsActive = false;
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã chuyển nhân viên vào thùng rác!";
            }

            return RedirectToAction(nameof(Index));
        }

        // 9. Khôi phục
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var emp = await _db.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (emp == null) return NotFound();

            if (emp.User != null)
            {
                emp.User.IsActive = true;
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Khôi phục nhân viên thành công!";
            }

            return RedirectToAction(nameof(Trash));
        }

        private void LoadAvailableUsers(int? selectedUserId = null)
        {
            var availableUsers = _db.Users
                .Where(u => u.IsActive
                            && u.RoleId == 3
                            && !_db.Employees.Any(e => e.UserId == u.UserId))
                .OrderBy(u => u.Username)
                .Select(u => new
                {
                    u.UserId,
                    DisplayText = u.Username + " - " + u.FullName
                })
                .ToList();

            ViewBag.UserId = new SelectList(availableUsers, "UserId", "DisplayText", selectedUserId);
        }
    }
}