using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafePos.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; } 
        public string OrderCode { get; set; } 

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.Now; 
        public int CreatedBy { get; set; } 
        [ForeignKey("CreatedBy")]
        public virtual User User { get; set; }

        public string Status { get; set; } 
        public decimal TotalAmount { get; set; } 
        public string PaymentStatus { get; set; } 

        public virtual ICollection<OrderItem> OrderItems { get; set; }
    }
}
