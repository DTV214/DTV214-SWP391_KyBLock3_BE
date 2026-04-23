namespace TetGift.BLL.Interfaces
{
    public interface IEmailTemplateRenderer
    {
        string RenderOtp(string otp, int minutes);
        string RenderQuotationApproved(string customerName, int quotationId, string quotationLink);

        string RenderOrderPaymentSuccess(
            string customerName,
            int orderId,
            string amount,
            string subtotalAmount,
            string discountAmount,
            string baseAmount,
            string vatAmount,
            string orderLink,
            string orderItemsHtml,
            string vatRequestInfoHtml
        );

        string RenderVatVerificationNotice(
            string recipientName,
            int orderId,
            string customerName,
            string customerPhone,
            string customerEmail,
            string customerAddress,
            string subtotalAmount,
            string discountAmount,
            string baseAmount,
            string vatAmount,
            string finalAmount,
            string vatCompanyName,
            string vatCompanyTaxCode,
            string vatCompanyAddress,
            string vatInvoiceEmail,
            string orderLink,
            string orderItemsHtml
        );

        string RenderOrderStatusChanged(string customerName, int orderId, string orderStatus, string orderLink, string orderItemsHtml);
    }
}
