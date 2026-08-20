namespace CafePos.Models.ViewModels
{
    public class PosOrderViewModel
    {
        public int? TableId { get; set; }

        public string? Note { get; set; }

        public List<PosOrderItemViewModel> Items { get; set; }
            = new List<PosOrderItemViewModel>();
    }

    public class PosOrderItemViewModel
    {
        public int ProductId { get; set; }

        public int? ProductSizeId { get; set; }

        public int Quantity { get; set; }

        public string? Note { get; set; }

        public List<int> ToppingIds { get; set; }
            = new List<int>();
    }
}