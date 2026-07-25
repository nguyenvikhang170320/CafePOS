using System.ComponentModel.DataAnnotations;

namespace CafePos.Models.ViewModels
{
    public class EmployeeVM
    {
        public int? UserId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string FullName { get; set; } = string.Empty;

        // 🌟 THUỘC TÍNH QUAN TRỌNG ĐỂ XÓA SẠCH LỖI CS1061 & CS0117
        public int? PositionId { get; set; }

        public string? Username { get; set; }
        public string? EmployeeCode { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
    }
}