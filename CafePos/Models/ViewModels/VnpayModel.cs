namespace CafePos.Models.ViewModels
{
    public class VnPayPaymentRequest
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string OrderInfo { get; set; } = string.Empty;
        public string? BankCode { get; set; }
        public string? Language { get; set; } = "vn";
    }

    public class VnPayPaymentResponse
    {
        public bool Success { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string? TransactionNo { get; set; }
        public string? ResponseCode { get; set; }
        public string? TransactionStatus { get; set; }
        public string? OrderInfo { get; set; }
        public decimal Amount { get; set; }
    }
}