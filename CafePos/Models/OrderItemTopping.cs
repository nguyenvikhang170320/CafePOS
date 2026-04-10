using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafePos.Models
{
    public class OrderItemTopping
    {
        [Key]
        public int Id { get; set; } 

        public int OrderItemId { get; set; } 
        [ForeignKey("OrderItemId")]
        public virtual OrderItem OrderItem { get; set; }

        public int ToppingId { get; set; } 
        [ForeignKey("ToppingId")]
        public virtual Topping Topping { get; set; }

        public decimal Price { get; set; } 
    }
}
