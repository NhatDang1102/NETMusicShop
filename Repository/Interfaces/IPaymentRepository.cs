using Repository.Models;

namespace Repository.Interfaces
{
    public interface IPaymentRepository
    {
        Task CreatePaymentAsync(Payment payment);
        Task UpdatePaymentStatusAsync(string transactionId, string status);
    }
}
