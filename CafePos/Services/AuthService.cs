using CafePos.Data;
using CafePos.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CafePos.Services
{
    public class AuthService : IAuthService
    {
        private readonly CafePosDbContext _context;

        public AuthService(CafePosDbContext context)
        {
            _context = context;
        }

        public async Task<User> LoginAsync(string username, string password)
        {
            // Tìm user có Username khớp và tài khoản đang Active
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            // Kiểm tra user có tồn tại không VÀ dùng BCrypt để so khớp mật khẩu
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return null; // Trả về null nếu sai tên đăng nhập hoặc sai mật khẩu
            }

            return user;
        }

        public async Task<User> RegisterAsync(User user, string password)
        {
            // Băm mật khẩu bằng BCrypt trước khi lưu vào CSDL
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.IsActive = true;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }
    }
}