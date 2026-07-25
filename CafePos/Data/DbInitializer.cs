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
                    new Role { Name = "Staff" },
                    new Role { Name = "Employee" }
                };

                context.Roles.AddRange(roles);
                context.SaveChanges();
            }

            // Seed Admin User
            if (!context.Users.Any())
            {
                var adminRole = context.Roles.First(r => r.Name == "Admin");
                var staffRole = context.Roles.First(r => r.Name == "Staff");
                var employeeRole = context.Roles.First(r => r.Name == "Employee");
                var adminUser = new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin@123"),
                    FullName = "Quản trị viên Hệ thống",
                    RoleId = adminRole.RoleId,
                    IsActive = true,
                    Email = "admincafe@gmail.com",
                    NgayCapNhat = DateTime.Now,
                    TrangThai = "Hoạt động"
                };


                var staffUser = new User
                {
                    Username = "Khang",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    FullName = "Nguyễn Vĩ Khang",
                    RoleId = staffRole.RoleId,
                    IsActive = true,
                    Email = "nguyenvikhang849@gmail.com",
                    NgayCapNhat = DateTime.Now,
                    TrangThai = "Hoạt động"
                };


                var employeeUser = new User
                {
                    Username = "Lan",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("1234567"),
                    FullName = "Nguyễn Ngọc Lan",
                    RoleId = employeeRole.RoleId,
                    IsActive = true,
                    Email = "nguyenngoclan@gmail.com",
                    NgayCapNhat = DateTime.Now,
                    TrangThai = "Hoạt động"
                };


                context.Users.AddRange(adminUser, staffUser, employeeUser); 
                context.SaveChanges();
            }
        }
    }
}