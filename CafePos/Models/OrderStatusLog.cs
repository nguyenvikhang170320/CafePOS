using System;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CafePos.Models
{
    public class OrderStatusLog
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; } = DateTime.Now;
        public string ChangedBy { get; set; } = string.Empty;

        [ValidateNever]
        public virtual Order? Order { get; set; }
    }
}