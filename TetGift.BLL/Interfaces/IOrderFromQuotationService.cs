namespace TetGift.BLL.Interfaces
{
    public interface IOrderFromQuotationService
    {
        //tạo order + orderdetails từ quotation
        Task<int> CreateOrderFromQuotationAsync(int quotationId, int accountId);
    }
}
