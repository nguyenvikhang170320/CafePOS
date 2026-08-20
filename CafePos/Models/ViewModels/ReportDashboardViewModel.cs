namespace CafePos.Models.ViewModels
{
    public class ReportDashboardViewModel
    {
        public int SelectedYear { get; set; }

        public decimal TotalRevenueThisYear { get; set; }

        public int TotalOrdersThisYear { get; set; }

        public List<RevenueByMonthViewModel> MonthlyRevenues { get; set; }
            = new List<RevenueByMonthViewModel>();

        public List<RevenueByYearViewModel> YearlyRevenues { get; set; }
            = new List<RevenueByYearViewModel>();

        public List<BestSellingProductViewModel> BestSellingProducts { get; set; }
            = new List<BestSellingProductViewModel>();

        public List<DailyRevenueViewModel> Last7DaysRevenues { get; set; }
            = new List<DailyRevenueViewModel>();

        // =========================================================
        // DOANH THU THEO NHÂN VIÊN
        // =========================================================
        public List<EmployeeRevenueViewModel> EmployeeRevenues { get; set; }
            = new List<EmployeeRevenueViewModel>();
    }
}