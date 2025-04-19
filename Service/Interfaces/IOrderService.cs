using Repository.DTOs;

namespace Service.Interfaces
{
    public interface IOrderService
    {
        Task<PayPalApprovalDto> CreateOrderWithPaypalAsync(string userEmail, OrderRequestDto dto);
        Task<bool> ConfirmOrderPaymentAsync(string paypalOrderId);
    }
}
