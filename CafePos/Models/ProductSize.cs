using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafePos.Models
{
    public class ProductSize
    {
        [Key]
        public int ProductSizeId { get; set; } 

        public int ProductId { get; set; } 
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        public string SizeName { get; set; } 
        public decimal ExtraPrice { get; set; }
    }
}
