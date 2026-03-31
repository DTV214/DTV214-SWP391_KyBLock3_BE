using TetGift.BLL.Interfaces;

namespace TetGift.BLL.Services
{
    public class FileEmailTemplateRenderer : IEmailTemplateRenderer
    {
        public string RenderOtp(string otp, int minutes)
        {
            // Try multiple possible paths
            var possiblePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "OtpEmail.html"),
                Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "OtpEmail.html"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates", "OtpEmail.html")
            };

            string? path = null;
            foreach (var possiblePath in possiblePaths)
            {
                if (File.Exists(possiblePath))
                {
                    path = possiblePath;
                    break;
                }
            }

            if (path == null || !File.Exists(path))
            {
                var searchedPaths = string.Join(", ", possiblePaths);
                throw new FileNotFoundException(
                    $"Email template 'OtpEmail.html' not found. Searched paths: {searchedPaths}. " +
                    $"Current directory: {Directory.GetCurrentDirectory()}, " +
                    $"Base directory: {AppContext.BaseDirectory}");
            }

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

            string? path = null;
            foreach (var possiblePath in possiblePaths)
            {
                if (File.Exists(possiblePath))
                {
                    path = possiblePath;
                    break;
                }
            }

            if (path == null || !File.Exists(path))
            {
                var searchedPaths = string.Join(", ", possiblePaths);
                throw new FileNotFoundException(
                    $"Email template 'QuotationApprovedEmail.html' not found. Searched paths: {searchedPaths}. " +
                    $"Current directory: {Directory.GetCurrentDirectory()}, " +
                    $"Base directory: {AppContext.BaseDirectory}");
            }

            var html = File.ReadAllText(path);

            return html.Replace("{{CUSTOMER_NAME}}", customerName)
                       .Replace("{{QUOTATION_ID}}", quotationId.ToString())
                       .Replace("{{QUOTATION_LINK}}", quotationLink);
        }

        public string RenderOrderPaymentSuccess(string customerName, int orderId, string amount, string orderLink)
        {
            var possiblePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "OrderPaymentSuccessEmail.html"),
                Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "OrderPaymentSuccessEmail.html"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates", "OrderPaymentSuccessEmail.html")
            };

            string? path = null;
            foreach (var possiblePath in possiblePaths)
            {
                if (File.Exists(possiblePath))
                {
                    path = possiblePath;
                    break;
                }
            }

            if (path == null || !File.Exists(path))
            {
                var searchedPaths = string.Join(", ", possiblePaths);
                throw new FileNotFoundException(
                    $"Email template 'OrderPaymentSuccessEmail.html' not found. Searched paths: {searchedPaths}. " +
                    $"Current directory: {Directory.GetCurrentDirectory()}, " +
                    $"Base directory: {AppContext.BaseDirectory}");
            }

            var html = File.ReadAllText(path);

            return html.Replace("{{CUSTOMER_NAME}}", customerName)
                       .Replace("{{ORDER_ID}}", orderId.ToString())
                       .Replace("{{AMOUNT}}", amount)
                       .Replace("{{ORDER_LINK}}", orderLink);
        }

        public string RenderOrderStatusChanged(string customerName, int orderId, string orderStatus, string orderLink, string orderItemsHtml)
        {
            var possiblePaths = new[]
            {
        Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "OrderStatusChangedEmail.html"),
        Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "OrderStatusChangedEmail.html"),
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates", "OrderStatusChangedEmail.html")
    };

            string? path = null;
            foreach (var possiblePath in possiblePaths)
            {
                if (File.Exists(possiblePath))
                {
                    path = possiblePath;
                    break;
                }
            }

            if (path == null || !File.Exists(path))
            {
                var searchedPaths = string.Join(", ", possiblePaths);
                throw new FileNotFoundException(
                    $"Email template 'OrderStatusChangedEmail.html' not found. Searched paths: {searchedPaths}. " +
                    $"Current directory: {Directory.GetCurrentDirectory()}, " +
                    $"Base directory: {AppContext.BaseDirectory}");
            }

            var html = File.ReadAllText(path);

            return html.Replace("{{CUSTOMER_NAME}}", customerName)
                       .Replace("{{ORDER_ID}}", orderId.ToString())
                       .Replace("{{ORDER_STATUS}}", orderStatus)
                       .Replace("{{ORDER_LINK}}", orderLink)
                       .Replace("{{ORDER_ITEMS}}", orderItemsHtml);
        }
    }
}
