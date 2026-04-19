using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IUnitOfWork _uow;
    private readonly IWebHostEnvironment _env;

    private const string SellerLegalName = "Tết Gift";
    private const string SellerTaxCode = "0312345678";
    private const string SellerAddress = "S603 Vinhomes GrandPark";
    private const string SellerPhone = "1900 1234";
    private const string SellerEmail = "support@tetgift.vn";

    public InvoiceService(IUnitOfWork uow, IWebHostEnvironment env)
    {
        _uow = uow;
        _env = env;
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(int orderId, int? accountId)
    {
        var order = await GetOrderAsync(orderId, accountId);
        return await BuildPdfByModeAsync(order, order.RequireVatInvoice ? InvoiceMode.Vat : InvoiceMode.Normal);
    }

    public async Task<byte[]> GenerateNormalInvoicePdfAsync(int orderId, int? accountId)
    {
        var order = await GetOrderAsync(orderId, accountId);
        return await BuildPdfByModeAsync(order, InvoiceMode.Normal);
    }

    public async Task<byte[]> GenerateVatInvoicePdfAsync(int orderId, int? accountId)
    {
        var order = await GetOrderAsync(orderId, accountId);

        if (!order.RequireVatInvoice)
            throw new Exception("Đơn hàng này không có hóa đơn VAT.");

        return await BuildPdfByModeAsync(order, InvoiceMode.Vat);
    }

    public async Task<string> GetDownloadFileNameAsync(int orderId, int? accountId)
    {
        var order = await GetOrderLightAsync(orderId, accountId);
        return order.RequireVatInvoice
            ? $"HoaDonVAT_{order.Orderid:D6}.pdf"
            : $"HoaDon_{order.Orderid:D6}.pdf";
    }

    public async Task<string> GetNormalInvoiceFileNameAsync(int orderId, int? accountId)
    {
        var order = await GetOrderLightAsync(orderId, accountId);
        return $"HoaDon_{order.Orderid:D6}.pdf";
    }

    public async Task<string> GetVatInvoiceFileNameAsync(int orderId, int? accountId)
    {
        var order = await GetOrderLightAsync(orderId, accountId);

        if (!order.RequireVatInvoice)
            throw new Exception("Đơn hàng này không có hóa đơn VAT.");

        return $"HoaDonVAT_{order.Orderid:D6}.pdf";
    }

    private async Task<Order> GetOrderAsync(int orderId, int? accountId)
    {
        var orderRepo = _uow.GetRepository<Order>();

        IQueryable<Order> query;
        if (accountId.HasValue)
        {
            query = orderRepo.Entities
                .Where(o => o.Orderid == orderId && o.Accountid == accountId.Value)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.ProductDetailProductparents)
                            .ThenInclude(pd => pd.Product)
                .Include(o => o.Promotion)
                .Include(o => o.Account);
        }
        else
        {
            query = orderRepo.Entities
                .Where(o => o.Orderid == orderId)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.ProductDetailProductparents)
                            .ThenInclude(pd => pd.Product)
                .Include(o => o.Promotion)
                .Include(o => o.Account);
        }

        var order = await query.FirstOrDefaultAsync();
        if (order == null)
            throw new Exception("Không tìm thấy đơn hàng.");

        return order;
    }

    private async Task<(int Orderid, bool RequireVatInvoice)> GetOrderLightAsync(int orderId, int? accountId)
    {
        var orderRepo = _uow.GetRepository<Order>();

        var order = await orderRepo.Entities
            .Where(o => o.Orderid == orderId && (!accountId.HasValue || o.Accountid == accountId.Value))
            .Select(o => new { o.Orderid, o.RequireVatInvoice })
            .FirstOrDefaultAsync();

        if (order == null)
            throw new Exception("Không tìm thấy đơn hàng.");

        return (order.Orderid, order.RequireVatInvoice);
    }

    private async Task<byte[]> BuildPdfByModeAsync(Order order, InvoiceMode mode)
    {
        var html = await BuildInvoiceHtmlAsync(order, mode);
        return await RenderPdfFromHtmlAsync(html);
    }

    private async Task<string> BuildInvoiceHtmlAsync(Order order, InvoiceMode mode)
    {
        var subTotal = CalculateSubTotal(order);
        var finalBaseAmount = order.Totalprice ?? subTotal;
        var discount = Math.Max(0, subTotal - finalBaseAmount);

        var isVat = mode == InvoiceMode.Vat;
        var vatRate = isVat ? (order.VatRate <= 0 ? 0.08m : order.VatRate) : 0m;
        var vatAmount = isVat ? order.VatAmount : 0m;
        var finalPayableAmount = finalBaseAmount + vatAmount;

        var templateFile = isVat ? "vat-invoice.html" : "normal-invoice.html";

        var templatePath = Path.Combine(
            _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
            "templates",
            "invoices",
            templateFile);

        if (!File.Exists(templatePath))
            throw new Exception($"Không tìm thấy template hóa đơn: {templatePath}");

        var template = await File.ReadAllTextAsync(templatePath, Encoding.UTF8);
        var created = order.Orderdatetime ?? DateTime.Now;

        var replacements = new Dictionary<string, string>
        {
            ["{{SellerLegalName}}"] = Html(SellerLegalName),
            ["{{SellerTaxCode}}"] = Html(SellerTaxCode),
            ["{{SellerAddress}}"] = Html(SellerAddress),
            ["{{SellerPhone}}"] = Html(SellerPhone),
            ["{{SellerEmail}}"] = Html(SellerEmail),

            ["{{DocumentNumber}}"] = Html(order.Orderid.ToString("D6")),
            ["{{CreatedDate}}"] = Html(created.ToString("dd/MM/yyyy HH:mm")),
            ["{{CreatedDay}}"] = Html(created.Day.ToString("00")),
            ["{{CreatedMonth}}"] = Html(created.Month.ToString("00")),
            ["{{CreatedYear}}"] = Html(created.Year.ToString()),
            ["{{OrderStatus}}"] = Html(TranslateStatus(order.Status)),

            ["{{BuyerName}}"] = Html(order.Customername ?? "N/A"),
            ["{{BuyerPhone}}"] = Html(order.Customerphone ?? "N/A"),
            ["{{BuyerEmail}}"] = Html(order.Customeremail ?? "N/A"),
            ["{{BuyerAddress}}"] = Html(order.Customeraddress ?? "N/A"),

            ["{{VatBuyerCompanyName}}"] = Html(order.VatCompanyName ?? order.Customername ?? "N/A"),
            ["{{VatBuyerTaxCode}}"] = Html(order.VatCompanyTaxCode ?? "N/A"),
            ["{{VatBuyerAddress}}"] = Html(order.VatCompanyAddress ?? order.Customeraddress ?? "N/A"),
            ["{{VatBuyerEmail}}"] = Html(order.VatInvoiceEmail ?? order.Customeremail ?? "N/A"),

            ["{{SubTotal}}"] = Html(FormatCurrency(subTotal)),
            ["{{FinalBaseAmount}}"] = Html(FormatCurrency(finalBaseAmount)),
            ["{{VatAmount}}"] = Html(FormatCurrency(vatAmount)),
            ["{{FinalPayableAmount}}"] = Html(FormatCurrency(isVat ? finalPayableAmount : finalBaseAmount)),
            ["{{VatRatePercent}}"] = Html($"{vatRate * 100:0}%"),
            ["{{FinalPayableAmountInWords}}"] = Html(NumberToVietnameseCurrency(isVat ? finalPayableAmount : finalBaseAmount)),

            ["{{ItemRows}}"] = BuildItemRows(order, isVat),
            ["{{DiscountRow}}"] = discount > 0
                ? $@"<tr>
                        <td>Chiết khấu / giảm giá</td>
                        <td>-{Html(FormatCurrency(discount))}</td>
                    </tr>"
                : string.Empty,

            ["{{NoteSection}}"] = string.IsNullOrWhiteSpace(order.Note)
                ? string.Empty
                : $@"<div class=""note-box"">
                        <div class=""label"">Ghi chú</div>
                        <div>{Html(order.Note)}</div>
                    </div>"
        };

        foreach (var item in replacements)
        {
            template = template.Replace(item.Key, item.Value);
        }

        return template;
    }

    private async Task<byte[]> RenderPdfFromHtmlAsync(string html)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var page = await browser.NewPageAsync();
        await page.SetContentAsync(html, new PageSetContentOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        return await page.PdfAsync(new PagePdfOptions
        {
            Format = "A4",
            PrintBackground = true,
            PreferCSSPageSize = true,
            Margin = new Margin
            {
                Top = "0mm",
                Bottom = "0mm",
                Left = "0mm",
                Right = "0mm"
            }
        });
    }

    private static decimal CalculateSubTotal(Order order)
    {
        decimal subTotal = 0m;

        if (order.OrderDetails != null)
        {
            foreach (var d in order.OrderDetails)
            {
                subTotal += d.Amount ?? ((d.Product?.Price ?? 0m) * (d.Quantity ?? 0));
            }
        }

        return subTotal;
    }

    private static string BuildItemRows(Order order, bool requireVat)
    {
        var columnCount = requireVat ? 6 : 5;

        if (order.OrderDetails == null || !order.OrderDetails.Any())
        {
            return $@"<tr><td colspan=""{columnCount}"" class=""center"">Không có dữ liệu hàng hóa</td></tr>";
        }

        var sb = new StringBuilder();
        var index = 1;

        foreach (var item in order.OrderDetails)
        {
            var productName = item.Product?.Productname ?? "N/A";
            var qty = item.Quantity ?? 0;
            var price = item.Product?.Price ?? 0m;
            var amount = item.Amount ?? (price * qty);
            var productSub = BuildProductSubText(item.Product);

            if (requireVat)
            {
                sb.Append($@"
<tr>
  <td class=""center"">{index}</td>
  <td>
    <div class=""product-name"">{Html(productName)}</div>
    {(string.IsNullOrWhiteSpace(productSub) ? string.Empty : $@"<div class=""product-sub"">{Html(productSub)}</div>")}
  </td>
  <td class=""center"">Cái</td>
  <td class=""center"">{qty}</td>
  <td class=""right"">{Html(FormatCurrency(price))}</td>
  <td class=""right"">{Html(FormatCurrency(amount))}</td>
</tr>");
            }
            else
            {
                sb.Append($@"
<tr>
  <td class=""center"">{index}</td>
  <td>
    <div class=""product-name"">{Html(productName)}</div>
    {(string.IsNullOrWhiteSpace(productSub) ? string.Empty : $@"<div class=""product-sub"">{Html(productSub)}</div>")}
  </td>
  <td class=""right"">{Html(FormatCurrency(price))}</td>
  <td class=""center"">{qty}</td>
  <td class=""right"">{Html(FormatCurrency(amount))}</td>
</tr>");
            }

            index++;
        }

        return sb.ToString();
    }

    private static string BuildProductSubText(Product? product)
    {
        if (product == null)
            return string.Empty;

        if (product.Configid == null || product.ProductDetailProductparents == null || !product.ProductDetailProductparents.Any())
            return string.Empty;

        var lines = product.ProductDetailProductparents
            .Where(pd => pd.Product != null)
            .Select(pd => $"- {pd.Product!.Productname} x{pd.Quantity ?? 0}");

        return "Thành phần hộp quà:\n" + string.Join("\n", lines);
    }

    private static string TranslateStatus(string? status)
    {
        return status?.ToUpper() switch
        {
            "PENDING" => "Chờ xác nhận",
            "CONFIRMED" => "Đã xác nhận",
            "PROCESSING" => "Đang xử lý",
            "SHIPPED" => "Đang giao",
            "DELIVERED" => "Đã giao",
            "CANCELLED" => "Đã hủy",
            "PAID_WAITING_STOCK" => "Đã thanh toán - Chờ hàng",
            _ => status ?? "N/A"
        };
    }

    private static string FormatCurrency(decimal amount)
    {
        return string.Format(CultureInfo.GetCultureInfo("vi-VN"), "{0:N0} đ", amount);
    }

    private static string Html(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }

    private static string NumberToVietnameseCurrency(decimal amount)
    {
        if (amount == 0)
            return "Không đồng";

        var number = (long)Math.Round(amount, 0, MidpointRounding.AwayFromZero);
        return ReadNumber(number) + " đồng";

        static string ReadNumber(long input)
        {
            var units = new[] { "", " nghìn", " triệu", " tỷ", " nghìn tỷ", " triệu tỷ" };
            if (input == 0) return "không";

            var parts = new List<string>();
            var unitIndex = 0;

            while (input > 0)
            {
                var block = (int)(input % 1000);
                if (block > 0)
                {
                    var text = ReadBlock(block);
                    if (!string.IsNullOrWhiteSpace(text))
                        parts.Insert(0, text + units[unitIndex]);
                }

                input /= 1000;
                unitIndex++;
            }

            var result = string.Join(" ", parts).Trim();
            if (string.IsNullOrWhiteSpace(result)) return "không";
            return char.ToUpper(result[0]) + result[1..];
        }

        static string ReadBlock(int number)
        {
            var digits = new[] { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
            var hundred = number / 100;
            var ten = (number % 100) / 10;
            var unit = number % 10;

            var result = new List<string>();

            if (hundred > 0)
            {
                result.Add(digits[hundred]);
                result.Add("trăm");
            }

            if (ten > 1)
            {
                result.Add(digits[ten]);
                result.Add("mươi");
                if (unit == 1) result.Add("mốt");
                else if (unit == 5) result.Add("lăm");
                else if (unit > 0) result.Add(digits[unit]);
            }
            else if (ten == 1)
            {
                result.Add("mười");
                if (unit == 5) result.Add("lăm");
                else if (unit > 0) result.Add(digits[unit]);
            }
            else if (ten == 0 && unit > 0)
            {
                if (hundred > 0) result.Add("lẻ");
                if (unit == 5 && hundred > 0) result.Add("năm");
                else result.Add(digits[unit]);
            }

            return string.Join(" ", result).Trim();
        }
    }

    private enum InvoiceMode
    {
        Normal = 1,
        Vat = 2
    }
}