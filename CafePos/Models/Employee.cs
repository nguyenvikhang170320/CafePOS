using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafePos.Models
{
    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; }

        // Khóa ngoại liên kết tới bảng Users
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        [StringLength(20)]
        public string EmployeeCode { get; set; } // Ví dụ: NV001

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public string? ImageUrl { get; set; } // Lưu link ảnh từ Cloudinary

        public DateTime HireDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;
    }
}