using TetGift.BLL.Interfaces;

namespace TetGift.BLL.Services
{
    public class FileEmailTemplateRenderer : IEmailTemplateRenderer
    {
        public string RenderOtp(string otp, int minutes)
        {
            var possiblePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "OtpEmail.html"),
                Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "OtpEmail.html"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates", "OtpEmail.html")
            };

            string? path = possiblePaths.FirstOrDefault(File.Exists);
            if (path == null)
                throw new FileNotFoundException("Email template 'OtpEmail.html' not found.");

            var html = File.ReadAllText(path);

            return html.Replace("{{OTP}}", otp)
                       .Replace("{{MINUTES}}", minutes.ToString());
        }

        public string RenderQuotationApproved(string customerName, int quotationId, string quotationLink)
        {
            var possiblePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "QuotationApprovedEmail.html"),
                Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "QuotationApprovedEmail.html"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates", "QuotationApprovedEmail.html")
            };

            string? path = possiblePaths.FirstOrDefault(File.Exists);
            if (path == null)
                throw new FileNotFoundException("Email template 'QuotationApprovedEmail.html' not found.");

            var html = File.ReadAllText(path);

            return html.Replace("{{CUSTOMER_NAME}}", customerName)
                       .Replace("{{QUOTATION_ID}}", quotationId.ToString())
                       .Replace("{{QUOTATION_LINK}}", quotationLink);
        }

        public string RenderOrderPaymentSuccess(
            string customerName,
            int orderId,
            string amount,
            string subtotalAmount,
            string discountAmount,
            string baseAmount,
            string vatAmount,
            string orderLink,
            string orderItemsHtml,
            string vatRequestInfoHtml)
        {
            var possiblePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "OrderPaymentSuccessEmail.html"),
                Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "OrderPaymentSuccessEmail.html"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates", "OrderPaymentSuccessEmail.html")
            };

            string? path = possiblePaths.FirstOrDefault(File.Exists);
            if (path == null)
                throw new FileNotFoundException("Email template 'OrderPaymentSuccessEmail.html' not found.");

            var html = File.ReadAllText(path);

            return html.Replace("{{CUSTOMER_NAME}}", customerName)
                       .Replace("{{ORDER_ID}}", orderId.ToString())
                       .Replace("{{AMOUNT}}", amount)
                       .Replace("{{SUBTOTAL_AMOUNT}}", subtotalAmount)
                       .Replace("{{DISCOUNT_AMOUNT}}", discountAmount)
                       .Replace("{{BASE_AMOUNT}}", baseAmount)
                       .Replace("{{VAT_AMOUNT}}", vatAmount)
                       .Replace("{{ORDER_LINK}}", orderLink)
                       .Replace("{{ORDER_ITEMS}}", orderItemsHtml)
                       .Replace("{{VAT_REQUEST_INFO}}", vatRequestInfoHtml);
        }

        public string RenderVatVerificationNotice(
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
            string orderItemsHtml)
        {
            var possiblePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "VatRequestVerificationEmail.html"),
                Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "VatRequestVerificationEmail.html"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates", "VatRequestVerificationEmail.html")
            };

            string? path = possiblePaths.FirstOrDefault(File.Exists);
            if (path == null)
                throw new FileNotFoundException("Email template 'VatRequestVerificationEmail.html' not found.");

            var html = File.ReadAllText(path);

            return html.Replace("{{RECIPIENT_NAME}}", recipientName)
                       .Replace("{{ORDER_ID}}", orderId.ToString())
                       .Replace("{{CUSTOMER_NAME}}", customerName)
                       .Replace("{{CUSTOMER_PHONE}}", customerPhone)
                       .Replace("{{CUSTOMER_EMAIL}}", customerEmail)
                       .Replace("{{CUSTOMER_ADDRESS}}", customerAddress)
                       .Replace("{{SUBTOTAL_AMOUNT}}", subtotalAmount)
                       .Replace("{{DISCOUNT_AMOUNT}}", discountAmount)
                       .Replace("{{BASE_AMOUNT}}", baseAmount)
                       .Replace("{{VAT_AMOUNT}}", vatAmount)
                       .Replace("{{FINAL_AMOUNT}}", finalAmount)
                       .Replace("{{VAT_COMPANY_NAME}}", vatCompanyName)
                       .Replace("{{VAT_COMPANY_TAX_CODE}}", vatCompanyTaxCode)
                       .Replace("{{VAT_COMPANY_ADDRESS}}", vatCompanyAddress)
                       .Replace("{{VAT_INVOICE_EMAIL}}", vatInvoiceEmail)
                       .Replace("{{ORDER_LINK}}", orderLink)
                       .Replace("{{ORDER_ITEMS}}", orderItemsHtml);
        }

        public string RenderOrderStatusChanged(string customerName, int orderId, string orderStatus, string orderLink, string orderItemsHtml)
        {
            var possiblePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "OrderStatusChangedEmail.html"),
                Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "OrderStatusChangedEmail.html"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates", "OrderStatusChangedEmail.html")
            };

            string? path = possiblePaths.FirstOrDefault(File.Exists);
            if (path == null)
                throw new FileNotFoundException("Email template 'OrderStatusChangedEmail.html' not found.");

            var html = File.ReadAllText(path);

            return html.Replace("{{CUSTOMER_NAME}}", customerName)
                       .Replace("{{ORDER_ID}}", orderId.ToString())
                       .Replace("{{ORDER_STATUS}}", orderStatus)
                       .Replace("{{ORDER_LINK}}", orderLink)
                       .Replace("{{ORDER_ITEMS}}", orderItemsHtml);
        }
    }
}
