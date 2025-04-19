using Microsoft.EntityFrameworkCore;
using Repository.Interfaces;
using Repository.Models;

namespace Repository.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly MusicShopDBContext _context;
        public PaymentRepository(MusicShopDBContext context)
        {
            _context = context;
        }

        public async Task CreatePaymentAsync(Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
        }

        public async Task UpdatePaymentStatusAsync(string transactionId, string status)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.TransactionId == transactionId);
            if (payment == null) return;
            payment.PaymentStatus = status;
            payment.PaidAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
