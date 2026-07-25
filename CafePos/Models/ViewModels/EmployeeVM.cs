namespace CafePos.Models.ViewModels
{
    public class EmployeeVM
    {
        public int? UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public int? PositionId { get; set; }

        public string? EmployeeCode { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public string? ImageUrl { get; set; }

        // Các thuộc tính dùng cho việc tự tạo Tài khoản User
        public string? Username { get; set; }

        public string? Password { get; set; }

        public string? Email { get; set; }
    }
}