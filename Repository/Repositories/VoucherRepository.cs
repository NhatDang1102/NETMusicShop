using Microsoft.EntityFrameworkCore;
using Repository.Interfaces;
using Repository.Models;

namespace Repository.Repositories
{
    public class VoucherRepository : IVoucherRepository
    {
        private readonly MusicShopDBContext _context;
        public VoucherRepository(MusicShopDBContext context)
        {
            _context = context;
        }

        public async Task<Voucher?> GetByCodeAsync(string code)
        {
            return await _context.Vouchers
                .FirstOrDefaultAsync(v => v.Code == code && v.ExpiredAt > DateTime.UtcNow);
        }

        public async Task UseVoucherAsync(Guid voucherId)
        {
            var voucher = await _context.Vouchers.FindAsync(voucherId);
            if (voucher == null) return;
            voucher.ExpiredAt = DateTime.UtcNow; 
            _context.Vouchers.Update(voucher);
            await _context.SaveChangesAsync();
        }
    }
}
