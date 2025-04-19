using Microsoft.EntityFrameworkCore;
using Repository.Interfaces;
using Repository.Models;

namespace Repository.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly MusicShopDBContext _context;

        public OrderRepository(MusicShopDBContext context)
        {
            _context = context;
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order?> GetOrderByIdAsync(Guid orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task UpdateOrderStatusAsync(Guid orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return;
            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task SaveOrderItemsAsync(List<OrderItem> items)
        {
            _context.OrderItems.AddRange(items);
            await _context.SaveChangesAsync();
        }

        public async Task<Order?> GetOrderByTransactionId(string transactionId)
        {
            return await _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o.OrderItems)
                .Include(p => p.Order)
                    .ThenInclude(o => o.User) 
                .Where(p => p.TransactionId == transactionId)
                .Select(p => p.Order)
                .FirstOrDefaultAsync();
        }

    }
}
