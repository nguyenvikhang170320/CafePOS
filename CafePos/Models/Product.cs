using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafePos.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; } 
        [Required]
        public string Name { get; set; }

        public int CategoryId { get; set; } 
        [ForeignKey("CategoryId")]
        [ValidateNever]
        public virtual Category Category { get; set; }

        public decimal BasePrice { get; set; }
        public string? ImageUrl { get; set; } 
        public bool IsActive { get; set; } = true; 
    }
}
