using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CafePos.Models.ViewModels
{
    public class EmployeeVM
    {
        [Required(ErrorMessage = "Vui lòng chọn tài khoản nhân viên")]
        public int? UserId { get; set; }

        public string? Username { get; set; }
        public string? Password { get; set; }

        public string? FullName { get; set; }

        public string? EmployeeCode { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }

        [ValidateNever]
        public string? ImageUrl { get; set; }
    }
}