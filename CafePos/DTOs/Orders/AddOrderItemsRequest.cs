namespace CafePos.DTOs.Orders
{
    public class AddOrderItemsRequest
    {
        public List<AddOrderItemRequest> Items { get; set; } = new();
    }

    public class AddOrderItemRequest
    {
        public int ProductId { get; set; }
        public int? ProductSizeId { get; set; }
        public int Quantity { get; set; }
        public string? Note { get; set; }
        public List<int> ToppingIds { get; set; } = new();
    }
}
