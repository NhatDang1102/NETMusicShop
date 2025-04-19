using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IPayPalService
    {
        Task<string> CreatePaymentUrl(decimal totalAmount);
    }
}
