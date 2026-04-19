namespace TetGift.BLL.Interfaces;

public interface IInvoiceService
{
    Task<byte[]> GenerateInvoicePdfAsync(int orderId, int? accountId);
    Task<byte[]> GenerateNormalInvoicePdfAsync(int orderId, int? accountId);
    Task<byte[]> GenerateVatInvoicePdfAsync(int orderId, int? accountId);

    Task<string> GetDownloadFileNameAsync(int orderId, int? accountId);
    Task<string> GetNormalInvoiceFileNameAsync(int orderId, int? accountId);
    Task<string> GetVatInvoiceFileNameAsync(int orderId, int? accountId);
}
