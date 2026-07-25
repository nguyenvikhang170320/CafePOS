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

        // Mã nhân viên thu ngân trực tiếp thực hiện thanh toán
        public int? EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        public string Method { get; set; } // Cash, Banking, VNPay, ...
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; } = DateTime.Now;

        // --- Các trường bổ sung cho VNPay ---
        public string? TransactionNo { get; set; }
        public string? OrderInfo { get; set; }
        public string? ResponseCode { get; set; }
        public bool IsSuccess { get; set; } = true;
    }
}