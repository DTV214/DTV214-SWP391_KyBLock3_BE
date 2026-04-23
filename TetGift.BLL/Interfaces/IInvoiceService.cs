namespace TetGift.BLL.Interfaces;

public interface IInvoiceService
{
    Task<byte[]> GenerateInvoicePdfAsync(int orderId, int? accountId);
    Task<string> GetDownloadFileNameAsync(int orderId, int? accountId);
}
