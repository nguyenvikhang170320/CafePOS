using Microsoft.AspNetCore.Mvc;
using CafePos.Data; // Thay bằng namespace DbContext của bạn
using CafePos.Models;
using CafePos.Models.ViewModels;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace CafePos.Areas.Admin.Controllers
{
    [Area("Admin")]
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

        [HttpPost]
        public async Task<IActionResult> CreateAsync(EmployeeVM obj, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                // 1. Xử lý ảnh (Giả sử bạn đã có logic Upload Cloudinary)
                if (file != null)
                {
                    var result = await _employeeService.AddPhotoAsync(file, "CafePos/Employee");
                    obj.ImageUrl = result.SecureUrl.ToString();
                }

                // 2. Tạo User trước
                User newUser = new User
                {
                    Username = obj.Username,
                    FullName = obj.FullName,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(obj.Password),
                    RoleId = 3, // Role nhân viên như bạn muốn
                    IsActive = true
                };

                using var transaction = _db.Database.BeginTransaction();
                try
                {
                    _db.Users.Add(newUser);
                    _db.SaveChanges(); // Lưu để lấy UserId tự tăng

                    // 3. Tạo Employee nối với UserId vừa tạo
                    Employee newEmp = new Employee
                    {
                        UserId = newUser.UserId,
                        EmployeeCode = obj.EmployeeCode ?? "NV" + newUser.UserId,
                        PhoneNumber = obj.PhoneNumber,
                        Address = obj.Address,
                        ImageUrl = obj.ImageUrl,
                        HireDate = DateTime.Now
                    };

                    _db.Employees.Add(newEmp);
                    _db.SaveChanges();

                    transaction.Commit();
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    transaction.Rollback();
                }
            }
            return View(obj);
        }

        // 2. Thùng rác (IsActive = false)
        public async Task<IActionResult> Trash()
        {
            var trashList = await _db.Employees
                .Include(e => e.User)
                .Where(e => e.User.IsActive == false)
                .ToListAsync();
            return View(trashList);
        }

        // 3. Chi tiết nhân viên
        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null) return NotFound();
            var employee = await _db.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.EmployeeId == id);
            if (employee == null) return NotFound();
            return View(employee);
        }

        // 4. Chỉnh sửa (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var emp = await _db.Employees.Include(e => e.User).FirstOrDefaultAsync(x => x.EmployeeId == id);
            if (emp == null) return NotFound();

            var vm = new EmployeeVM
            {
                FullName = emp.User.FullName,
                Username = emp.User.Username,
                EmployeeCode = emp.EmployeeCode,
                PhoneNumber = emp.PhoneNumber,
                Address = emp.Address,
                ImageUrl = emp.ImageUrl
            };
            return View(vm);
        }

        // 5. Chỉnh sửa (POST)
        [HttpPost]
        public async Task<IActionResult> Edit(int id, EmployeeVM obj, IFormFile? file)
        {
            var empDb = await _db.Employees.Include(e => e.User).FirstOrDefaultAsync(x => x.EmployeeId == id);
            if (empDb == null) return NotFound();

            if (ModelState.IsValid)
            {
                // Update User info
                empDb.User.FullName = obj.FullName;
                // Update Employee info
                empDb.PhoneNumber = obj.PhoneNumber;
                empDb.Address = obj.Address;
                empDb.EmployeeCode = obj.EmployeeCode;

                if (file != null)
                {
                    // Logic upload Cloudinary ở đây giống Product
                    var result = await _employeeService.AddPhotoAsync(file, "CafePos/Employee");
                    obj.ImageUrl = result.SecureUrl.ToString();
                }

                _db.Update(empDb);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(obj);
        }

        // 6. Xóa tạm thời (Đưa vào thùng rác)
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _db.Users.FindAsync(id); // id này là UserId
            if (user != null)
            {
                user.IsActive = false;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // 7. Khôi phục từ thùng rác
        public async Task<IActionResult> Restore(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = true;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Trash));
        }
    }
}