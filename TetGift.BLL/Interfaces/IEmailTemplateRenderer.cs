namespace TetGift.BLL.Interfaces
{
    public interface IEmailTemplateRenderer
    {
        string RenderOtp(string otp, int minutes);
        string RenderQuotationApproved(string customerName, int quotationId, string quotationLink);
        string RenderOrderPaymentSuccess(string customerName, int orderId, string amount, string orderLink);
    }
}
