using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafePos.Models
{
    public class PasswordResetToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string Token { get; set; }

        public DateTime ExpiredAt { get; set; }

        public bool IsUsed { get; set; } = false;

        [ValidateNever]
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}