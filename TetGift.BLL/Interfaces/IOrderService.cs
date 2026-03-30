using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces;

public interface IOrderService
{
    // Customer endpoints
    Task<OrderResponseDto> CreateOrderFromCartAsync(int accountId, CreateOrderRequest request);
    //Task<IEnumerable<OrderResponseDto>> GetOrdersByAccountIdAsync(int accountId);
    Task<OrderResponseDto> GetOrderByIdAsync(int orderId, int accountId);
    Task<OrderResponseDto> CancelOrderAsync(int orderId, int accountId, string userRole);
    Task<OrderResponseDto> UpdateOrderShippingInfoAsync(int orderId, int accountId, string userRole, UpdateOrderShippingRequest request);

    // Admin endpoints
    Task<PagedResponse<OrderResponseDto>> GetAllOrdersAsync(OrderQueryParameters queryParams);
    Task<OrderResponseDto> GetOrderByIdForAdminAsync(int orderId);
    Task<OrderResponseDto> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request, int accountId, string userRole);

    //New Ko bik đặt gì
    Task TryAllocateStockAfterPaymentAsync(int orderId);
    Task AllocateStockForWaitingOrderAsync(int orderId);
    Task ForceAllocateStockAsync(int orderId, int actorAccountId, string actorRole);
}
