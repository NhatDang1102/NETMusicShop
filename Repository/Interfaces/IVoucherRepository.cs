using Repository.Models;

namespace Repository.Interfaces
{
    public interface IVoucherRepository
    {
        Task<Voucher?> GetByCodeAsync(string code);
        Task UseVoucherAsync(Guid voucherId);
    }
}
