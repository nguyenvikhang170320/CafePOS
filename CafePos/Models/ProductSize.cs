using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafePos.Models
{
    public class ProductSize
    {
        [Key]
        public int ProductSizeId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn sản phẩm")]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        [ValidateNever]
        public virtual Product Product { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên size")]
        [StringLength(50)]
        public string SizeName { get; set; }

        [Range(0, 999999, ErrorMessage = "Giá cộng thêm phải lớn hơn hoặc bằng 0")]
        public decimal ExtraPrice { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}