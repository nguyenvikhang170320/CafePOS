namespace CafePos.Models.ViewModels
{
    public class EmployeeRevenueViewModel
    {
        public int? EmployeeId { get; set; }

        public string EmployeeName { get; set; } = string.Empty;

        public string PositionName { get; set; } = string.Empty;

        // Tổng số giao dịch Payment
        public int TotalTransactions { get; set; }

        // Số Order khác nhau nhân viên đã thu tiền
        public int TotalOrders { get; set; }

        // Tiền mặt
        public decimal CashRevenue { get; set; }

        // Chuyển khoản
        public decimal BankingRevenue { get; set; }

        // Thẻ
        public decimal CardRevenue { get; set; }

        // VNPAY
        public decimal VnPayRevenue { get; set; }

        // Các phương thức khác nếu sau này bổ sung
        public decimal OtherRevenue { get; set; }

        // Tổng tiền nhân viên đã trực tiếp thu
        public decimal TotalRevenue { get; set; }
    }
}