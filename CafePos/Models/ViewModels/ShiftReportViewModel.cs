using System;
using System.Collections.Generic;

namespace CafePos.Models.ViewModels
{
    public class ShiftReportViewModel
    {
        public DateTime ReportDate { get; set; } = DateTime.Today;
        public string EmployeeName { get; set; } = string.Empty;

        // Tổng quan trong ngày/ca
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }

        // Phân loại theo hình thức thanh toán (để nhân viên đếm tiền mặt két)
        public decimal CashRevenue { get; set; }
        public decimal BankTransferRevenue { get; set; }

        // Danh sách các đơn hàng đã xử lý trong ngày
        public List<OrderSummaryItem> TodayOrders { get; set; } = new();
    }

    public class OrderSummaryItem
    {
        public int OrderId { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
    }
}
