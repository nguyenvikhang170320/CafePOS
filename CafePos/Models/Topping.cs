using System.ComponentModel.DataAnnotations;

namespace CafePos.Models
{
    public class Topping
    {
        [Key]
        public int ToppingId { get; set; } 

        [Required]
        public string Name { get; set; } 

        public decimal Price { get; set; } 

        public bool IsActive { get; set; } = true;
    }
}
