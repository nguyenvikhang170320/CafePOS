using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafePos.Models
{
    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; } 

        public int OrderId { get; set; } 
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        public int? ProductSizeId { get; set; } 

        public int Quantity { get; set; } 
        public decimal UnitPrice { get; set; } 
        public string Note { get; set; } 
    }
}
