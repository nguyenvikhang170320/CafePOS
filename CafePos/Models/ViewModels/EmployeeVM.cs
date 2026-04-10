using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CafePos.Models.ViewModels
{
    public class EmployeeVM
    {
        // Thông tin bảng User
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "V.lòng nhập họ tên")]
        public string FullName { get; set; }

        // Thông tin bảng Employee
        public string? EmployeeCode { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }

        [ValidateNever]
        public string? ImageUrl { get; set; }
    }
}