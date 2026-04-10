using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafePos.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public int OrderId { get; set; } 
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        public string Method { get; set; }
        public decimal Amount { get; set; } 
        public DateTime PaidAt { get; set; } = DateTime.Now; 
    }
}
