namespace CafePos.Models.ViewModels
{
    public class DailyRevenueViewModel
    {
        public DateTime Date { get; set; }
        public string DateFormatted => Date.ToString("dd/MM"); // Dùng để hiển thị lên trục X của biểu đồ
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
    }
}
