using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafePos.Models
{
    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; }

        public int UserId { get; set; }

        [ValidateNever]
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required]
        [StringLength(20)]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        // 🌟 SỬA THÀNH int? ĐỂ KHÔNG BẮT BUỘC NHẬP NGAY LÚC TẠO ĐẦU TIÊN
        public int? PositionId { get; set; }

        [ValidateNever]
        [ForeignKey("PositionId")]
        public virtual Position? Position { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public string? ImageUrl { get; set; }

        public DateTime HireDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;
    }
}