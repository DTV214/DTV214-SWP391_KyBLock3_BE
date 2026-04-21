using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces;

public interface IOrderService
{
    Task<OrderResponseDto> CreateOrderFromCartAsync(int accountId, CreateOrderRequest request);
    Task<PagedResponse<OrderResponseDto>> GetAllOrdersAsync(OrderQueryParameters queryParams);
    Task<OrderResponseDto> UpdateOrderShippingInfoAsync(int orderId, int accountId, string userRole, UpdateOrderShippingRequest request);
    Task<OrderResponseDto> GetOrderByIdAsync(int orderId, int accountId);
    Task<OrderResponseDto> GetOrderByIdForAdminAsync(int orderId);
    Task<OrderResponseDto> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request, int accountId, string userRole);

    // Hàm dành cho khách hàng yêu cầu hủy
    Task<OrderResponseDto> CancelOrderAsync(int orderId, int accountId, string userRole);

    // HÀM MỚI: Dành cho Admin duyệt hoàn tiền và chính thức hủy đơn
    Task<OrderResponseDto> AdminApproveRefundAsync(int orderId, int adminAccountId);

    Task TryAllocateStockAfterPaymentAsync(int orderId);
    Task AllocateStockForWaitingOrderAsync(int orderId);
    Task ForceAllocateStockAsync(int orderId, int actorAccountId, string actorRole);

    // Product association analysis: products frequently bought together with given product
    Task<List<ProductAssociationDto>> GetProductAssociationsAsync(int productId, int top = 10, int minSupport = 1);
}