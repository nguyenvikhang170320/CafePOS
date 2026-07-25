using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CafePos.Models
{
    public class Position
    {
        [Key]
        public int PositionId { get; set; }

        [Required]
        [StringLength(100)]
        public string PositionName { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        [ValidateNever]
        public virtual ICollection<Employee> Employees { get; set; }
            = new List<Employee>();
    }
}

