namespace CafePos.DTOs.Orders
{
    public class CreateOrderRequest
    {
        public int? TableId { get; set; }
        public string OrderType { get; set; } = "DineIn";
        public string? Note { get; set; }
        public List<CreateOrderItemRequest> Items { get; set; } = new();
    }
    public class CreateOrderItemRequest
    {
        public int ProductId { get; set; }
        public int? ProductSizeId { get; set; }
        public int Quantity { get; set; } = 1;
        public string? Note { get; set; }
        public List<int> ToppingIds { get; set; } = new();
    }
    public class UpdateOrderStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
    public class OrderSummaryDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string? TableName { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
