using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces;

public interface IOrderService
{
    // Customer endpoints
    Task<OrderResponseDto> CreateOrderFromCartAsync(int accountId, CreateOrderRequest request);
    Task<IEnumerable<OrderResponseDto>> GetOrdersByAccountIdAsync(int accountId);
    Task<OrderResponseDto> GetOrderByIdAsync(int orderId, int accountId);

    // Admin endpoints
    Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync(string? status = null);
    Task<OrderResponseDto> GetOrderByIdForAdminAsync(int orderId);
    Task<OrderResponseDto> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request);
}
