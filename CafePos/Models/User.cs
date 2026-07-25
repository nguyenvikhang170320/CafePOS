using CafePos.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafePos.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public int RoleId { get; set; }

        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime NgayCapNhat { get; set; }
        public string TrangThai { get; set; }

        [ValidateNever]
        public virtual Employee? Employee { get; set; }
    }
}