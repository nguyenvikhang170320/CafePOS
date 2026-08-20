using CafePos.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CafePos.Data
{
    public static class DbInitializer
    {
        public static void Initialize(CafePosDbContext context)
        {
            // 1. Tự động áp dụng các file Migration (Tương đương lệnh Update-Database)
            context.Database.Migrate();

            // 2. Kiểm tra xem bảng Roles đã có dữ liệu chưa. Nếu có rồi thì chuyển qua kiểm tra bảng user.
            // Seed Roles
            if (!context.Roles.Any())
            {
                var roles = new Role[]
                {
                    new Role { Name = "Admin" },
                    new Role { Name = "Customer" },
                    new Role { Name = "Employee" }
                };

                context.Roles.AddRange(roles);
                context.SaveChanges();
            }
            if (!context.Positions.Any())
            {
                context.Positions.AddRange(
                     new Position
                     {
                         PositionName = "Admin",
                         Description = "Chủ quán",
                         IsActive = true
                     },
                    new Position
                    {
                        PositionName = "Quản lý",
                        Description = "Quản lý quán",
                        IsActive = true
                    },
                    new Position
                    {
                        PositionName = "Thu ngân",
                        Description = "Thu ngân",
                        IsActive = true
                    },
                    new Position
                    {
                        PositionName = "Pha chế",
                        Description = "Nhân viên pha chế",
                        IsActive = true
                    },
                    new Position
                    {
                        PositionName = "Phục vụ",
                        Description = "Nhân viên phục vụ",
                        IsActive = true
                    }
                );

                context.SaveChanges();
            }
            // Seed Users & Employees Đồng Bộ
            if (!context.Users.Any())
            {
                var adminRole = context.Roles.First(r => r.Name == "Admin");
                var staffRole = context.Roles.First(r => r.Name == "Customer"); // Hoặc KhachHang nếu bạn dùng Role này
                var employeeRole = context.Roles.First(r => r.Name == "Employee");
                var admin = context.Positions.First(x => x.PositionName == "Admin");
                var quanLy = context.Positions.First(x => x.PositionName == "Quản lý");
                var thuNgan = context.Positions.First(x => x.PositionName == "Thu ngân");
                var phucVu = context.Positions.First(x => x.PositionName == "Phục vụ");
                var phaChe = context.Positions.First(x => x.PositionName == "Pha chế");

                // 1. Tạo các tài khoản User
                var adminUser = new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin@123"),
                    RoleId = adminRole.RoleId,
                    Email = "admincafe@gmail.com",
                    IsActive = true,
                    TrangThai = "Hoạt động",
                    NgayCapNhat = DateTime.Now
                };

                var staffUser = new User // Account Khách hàng để test
                {
                    Username = "Khang",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    RoleId = staffRole.RoleId,
                    Email = "nguyenvikhang849@gmail.com",
                    IsActive = true,
                    TrangThai = "Hoạt động",
                    NgayCapNhat = DateTime.Now
                };
                var managerUser = new User
                {
                    Username = "nhanvienquanly",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("1234567"),
                    RoleId = employeeRole.RoleId,
                    Email = "nhanvienquanly@gmail.com",
                    IsActive = true,
                    TrangThai = "Hoạt động",
                    NgayCapNhat = DateTime.Now
                };

                var employeeUser = new User
                {
                    Username = "nhanvienphucvu",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("1234567"),
                    RoleId = employeeRole.RoleId,
                    Email = "nhanvienphucvu@gmail.com",
                    IsActive = true,
                    TrangThai = "Hoạt động",
                    NgayCapNhat = DateTime.Now
                };
                var employeeUser1 = new User
                {
                    Username = "nhanvienphache",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("1234567"),
                    RoleId = employeeRole.RoleId,
                    Email = "nhanvienphache@gmail.com",
                    IsActive = true,
                    TrangThai = "Hoạt động",
                    NgayCapNhat = DateTime.Now
                };
                var employeeUser2 = new User
                {
                    Username = "nhanvienthungan",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("1234567"),
                    RoleId = employeeRole.RoleId,
                    Email = "nhanvienthungan@gmail.com",
                    IsActive = true,
                    TrangThai = "Hoạt động",
                    NgayCapNhat = DateTime.Now
                };


                context.Users.AddRange(adminUser,staffUser,managerUser,employeeUser,employeeUser1,employeeUser2);
                context.SaveChanges();

                // 2. Chỉ tạo Hồ sơ Employee cho Admin và Nhân viên (BỎ KHANG RA)
                context.Employees.AddRange(
                    new Employee
                    {
                        UserId = adminUser.UserId,
                        FullName = "Quản Trị Viên",
                        EmployeeCode = "NV000",
                        PositionId = admin.PositionId,
                        PhoneNumber = "0909999999",
                        Address = "Phong Hòa, Đồng Tháp",
                        HireDate = DateTime.Now,
                        IsActive = true
                    },
                    new Employee
                    {
                        UserId = employeeUser1.UserId,
                        FullName = "Pha chế",
                        EmployeeCode = "NV002",
                        PositionId = phaChe.PositionId,
                        PhoneNumber = "0900000000",
                        Address = "Đồng Tháp",
                        HireDate = DateTime.Now,
                        IsActive = true
                    },
                    new Employee
                    {
                        UserId = employeeUser2.UserId,
                        FullName = "Thu Ngân",
                        EmployeeCode = "NV003",
                        PositionId = thuNgan.PositionId,
                        PhoneNumber = "0900000003",
                        Address = "Đồng Tháp",
                        HireDate = DateTime.Now,
                        IsActive = true
                    },
                    new Employee
                    {
                        UserId = employeeUser.UserId,
                        FullName = "Phục vụ",
                        EmployeeCode = "NV004",
                        PositionId = phucVu.PositionId,
                        PhoneNumber = "0900000004",
                        Address = "Đồng Tháp",
                        HireDate = DateTime.Now,
                        IsActive = true
                    },
                    new Employee
                    {
                        UserId = managerUser.UserId,
                        FullName = "Quản lý",
                        EmployeeCode = "NV001",
                        PositionId = quanLy.PositionId,
                        PhoneNumber = "0900000001",
                        Address = "Đồng Tháp",
                        HireDate = DateTime.Now,
                        IsActive = true
                    }
                );

                context.SaveChanges();
            }
        }
    }
}