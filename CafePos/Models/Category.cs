using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CafePos.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        public string Name { get; set; }

        public bool IsActive { get; set; } = true;

        // Mở lại cái này để bỏ qua kiểm tra khi tạo/sửa Category
        [ValidateNever]
        public virtual ICollection<Product> Products { get; set; }
    }
}