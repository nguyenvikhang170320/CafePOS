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

            // 2. Kiểm tra xem bảng Roles đã có dữ liệu chưa. Nếu có rồi thì dừng lại.
            if (context.Roles.Any())
            {
                return;   // Database đã được seed rồi
            }

            // 3. Nếu chưa có, tiến hành tạo sẵn dữ liệu cho bảng Roles
            var roles = new Role[]
            {
                // Lưu ý: Tùy theo cấu trúc Model của bạn, nếu RoleId là tự tăng (Identity) 
                // thì không cần gán RoleId = 1, 2. Ở đây mình làm chuẩn theo DB của bạn.
                new Role { Name = "Admin" },
                new Role { Name = "Staff" },
                new Role { Name = "Employee"}
            };

            context.Roles.AddRange(roles);
            context.SaveChanges(); // Lưu Role xuống DB trước để lấy ID

            // 4. Tạo sẵn 1 tài khoản Admin mặc định để test đăng nhập
            if (!context.Users.Any())
            {
                var adminRole = context.Roles.FirstOrDefault(r => r.Name == "Admin");

                var adminUser = new User
                {
                    Username = "admin",
                    // Mã hóa mật khẩu "123456" bằng thư viện BCrypt bạn vừa cài
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin@123"),
                    FullName = "Quản trị viên Hệ thống",
                    RoleId = adminRole.RoleId,
                    IsActive = true
                };

                context.Users.Add(adminUser);
                context.SaveChanges();
            }
        }
    }
}