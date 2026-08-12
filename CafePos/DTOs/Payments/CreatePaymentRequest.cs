namespace CafePos.DTOs.Payments
{
    public class CreatePaymentRequest
    {
        public string Method { get; set; } = "Cash";
        public decimal Amount { get; set; }
        public string? TransactionNo { get; set; }
    }
}
