namespace CafePos.Models.ViewModels
{
    public class ReportDashboardViewModel
    {
        public int SelectedYear { get; set; }
        public decimal TotalRevenueThisYear { get; set; }
        public int TotalOrdersThisYear { get; set; }

        public List<RevenueByMonthViewModel> MonthlyRevenues { get; set; } = new();
        public List<RevenueByYearViewModel> YearlyRevenues { get; set; } = new();
        public List<BestSellingProductViewModel> BestSellingProducts { get; set; } = new();

        // 🌟 Thêm thuộc tính này cho biểu đồ 7 ngày
        public List<DailyRevenueViewModel> Last7DaysRevenues { get; set; } = new();
    }
}
