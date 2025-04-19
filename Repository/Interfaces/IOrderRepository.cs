using Repository.Models;

namespace Repository.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order> CreateOrderAsync(Order order);
        Task<Order?> GetOrderByIdAsync(Guid orderId);
        Task UpdateOrderStatusAsync(Guid orderId, string status);
        Task SaveOrderItemsAsync(List<OrderItem> items);
        Task<Order?> GetOrderByTransactionId(string transactionId);

    }
}
