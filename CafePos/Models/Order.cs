using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;

namespace CafePos.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        public string OrderCode { get; set; } = string.Empty;

        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }

        public int UserId { get; set; }
        [ValidateNever]
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Precision(18, 2)]
        public decimal SubTotal { get; set; }

        [Precision(18, 2)]
        public decimal DiscountAmount { get; set; }

        [Precision(18, 2)]
        public decimal TotalAmount { get; set; }

        public string OrderStatus { get; set; } = "Pending";
        public string PaymentStatus { get; set; } = "Unpaid";
        public string? PaymentMethod { get; set; }
        public string? Note { get; set; }

        // 🌟 BỔ SUNG 2 DÒNG NÀY ĐỂ HẾT LỖI CS1061
        public int? TableId { get; set; }

        [ValidateNever]
        [ForeignKey("TableId")]
        public virtual Table? Table { get; set; }

        [ValidateNever]
        public virtual ICollection<OrderStatusLog>? OrderStatusLogs { get; set; }

        [ValidateNever]
        public virtual ICollection<OrderItem>? OrderItems { get; set; }

        [ValidateNever]
        public virtual ICollection<Payment>? Payments { get; set; }
    }
}