using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Collections.Generic;

namespace CafePos.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string? ImageUrl { get; set; }
        public int CategoryId { get; set; }
        public bool IsActive { get; set; } = true;

        [ValidateNever]
        public virtual Category? Category { get; set; }

        // Mối quan hệ 1-N tới ProductSize
        [ValidateNever]
        public virtual ICollection<ProductSize>? ProductSizes { get; set; }
    }
}