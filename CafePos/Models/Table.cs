using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CafePos.Models
{
    public class Table
    {
        public int TableId { get; set; }
        public string TableName { get; set; } = string.Empty; // VD: Bàn 01, Bàn 02

        // 🌟 BỔ SUNG MỚI: Cột phân Khu / Tầng
        public string? Area { get; set; } // VD: Khu A, Khu B, Tầng 1, Sân vườn...

        public bool IsActive { get; set; } = true;

        [ValidateNever]
        public virtual ICollection<Order>? Orders { get; set; }
    }
}