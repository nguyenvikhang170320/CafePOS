using CafePos.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CafePos.Services
{
    public interface IOrderService
    {
        Task<IEnumerable<Order>> GetAllOrdersAsync();
        Task<Order> GetOrderByIdAsync(int id);
        Task<Order> CreateOrderAsync(Order order);
        Task UpdateOrderStatusAsync(int orderId, string status);
    }
}