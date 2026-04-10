using CafePos.Data;
using CafePos.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace CafePos.Services
{
    public class OrderService : IOrderService
    {
        private readonly CafePosDbContext _context;

        public OrderService(CafePosDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            // Lấy danh sách đơn hàng kèm chi tiết bên trong
            return await _context.Orders
                .Include(o => o.OrderItems)
                .ToListAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
           // Tại đây sẽ chứa logic tính tổng tiền (TotalAmount) ở backend 
           // order.TotalAmount = order.OrderItems.Sum(x => x.Quantity * x.UnitPrice);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
            }
        }
    }
}