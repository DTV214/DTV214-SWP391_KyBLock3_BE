namespace TetGift.BLL.Interfaces;

public interface IInvoiceService
{
    /// <summary>
    /// Tạo PDF hóa đơn cho đơn hàng.
    /// </summary>
    /// <param name="orderId">ID đơn hàng</param>
    /// <param name="accountId">Nếu null → Admin xem mọi order, nếu có → chỉ xem order của chính mình</param>
    Task<byte[]> GenerateInvoicePdfAsync(int orderId, int? accountId);
}
