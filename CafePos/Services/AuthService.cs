using CafePos.Data;
using CafePos.Models;
using Microsoft.EntityFrameworkCore;

namespace CafePos.Services
{
    public class AuthService : IAuthService
    {
        private readonly CafePosDbContext _context;

        public AuthService(CafePosDbContext context)
        {
            _context = context;
        }

        public async Task<User?> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            username = username.Trim();

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive == true);

            if (user == null)
                return null;

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
                return null;

            bool isValidPassword = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!isValidPassword)
                return null;

            return user;
        }

        public async Task<User> RegisterAsync(User user, string password)
        {
            if (user == null)
                throw new Exception("Dữ liệu người dùng không hợp lệ.");

            if (string.IsNullOrWhiteSpace(user.Username))
                throw new Exception("Tên đăng nhập không được để trống.");

            if (string.IsNullOrWhiteSpace(user.Email))
                throw new Exception("Email không được để trống.");

            if (string.IsNullOrWhiteSpace(password))
                throw new Exception("Mật khẩu không được để trống.");

            user.Username = user.Username.Trim();
            user.FullName = user.FullName?.Trim();
            user.Email = user.Email?.Trim();

            bool existedUsername = await _context.Users.AnyAsync(x => x.Username == user.Username);
            if (existedUsername)
                throw new Exception("Tên đăng nhập đã tồn tại.");

            bool existedEmail = await _context.Users.AnyAsync(x => x.Email == user.Email);
            if (existedEmail)
                throw new Exception("Email đã tồn tại.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.IsActive = true;
            user.TrangThai = "Hoạt động";
            user.NgayCapNhat = DateTime.Now;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }
    }
}