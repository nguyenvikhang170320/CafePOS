namespace CafePos.DTOs.Menu
{
    public class MenuResponseDto
    {
        public List<MenuCategoryDto> Categories { get; set; } = new();
        public List<ToppingDto> Toppings { get; set; } = new();
    }
    public class MenuCategoryDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<MenuProductDto> Products { get; set; } = new();
    }
    public class MenuProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string? ImageUrl { get; set; }
        public List<ProductSizeDto> Sizes { get; set; } = new();
    }
    public class ProductSizeDto
    {
        public int ProductSizeId { get; set; }
        public string SizeName { get; set; } = string.Empty;
        public decimal ExtraPrice { get; set; }
    }
    public class ToppingDto
    {
        public int ToppingId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
    public class TableDto
    {
        public int TableId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string? Area { get; set; }
        public bool IsActive { get; set; }

        // Trạng thái này KHÔNG nằm trong DB.
        // API tự tính từ Orders.
        public string Status { get; set; } = "Available";
    }
}
