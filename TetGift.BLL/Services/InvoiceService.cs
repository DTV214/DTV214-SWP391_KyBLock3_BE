using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IUnitOfWork _uow;

    private const string SellerLegalName = "Tết Gift";
    private const string SellerTaxCode = "0312345678";
    private const string SellerAddress = "S603 Vinhomes GrandPark";
    private const string SellerPhone = "1900 1234";
    private const string SellerEmail = "support@tetgift.vn";

    public InvoiceService(IUnitOfWork uow)
    {
        _uow = uow;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(int orderId, int? accountId)
    {
        var orderRepo = _uow.GetRepository<Order>();

        IQueryable<Order> query;
        if (accountId.HasValue)
        {
            query = orderRepo.Entities
                .Where(o => o.Orderid == orderId && o.Accountid == accountId.Value)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(o => o.Promotion)
                .Include(o => o.Account);
        }
        else
        {
            query = orderRepo.Entities
                .Where(o => o.Orderid == orderId)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(o => o.Promotion)
                .Include(o => o.Account);
        }

        var order = await query.FirstOrDefaultAsync();
        if (order == null)
            throw new Exception("Không tìm thấy đơn hàng.");

        return GeneratePdf(order);
    }

    private byte[] GeneratePdf(Order order)
    {
        var primaryColor = "#690000";
        var goldColor = "#D4AF37";
        var grayText = "#4B5563";

        var subTotal = 0m;
        if (order.OrderDetails != null)
        {
            foreach (var d in order.OrderDetails)
                subTotal += d.Amount ?? ((d.Product?.Price ?? 0) * (d.Quantity ?? 0));
        }

        var finalBaseAmount = order.Totalprice ?? subTotal;
        var discount = subTotal - finalBaseAmount;
        if (discount < 0) discount = 0;

        var requireVat = order.RequireVatInvoice;
        var vatRate = requireVat ? (order.VatRate <= 0 ? 0.08m : order.VatRate) : 0m;
        var vatAmount = requireVat ? order.VatAmount : 0m;
        var finalPayableAmount = finalBaseAmount + vatAmount;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Helvetica"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();

        void ComposeHeader(IContainer container)
        {
            container.PaddingBottom(12).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text(SellerLegalName).FontSize(22).Bold().FontColor(primaryColor);
                        left.Item().Text(requireVat
                                ? "Chứng từ nội bộ mô phỏng hóa đơn GTGT"
                                : "Hóa đơn mua hàng")
                            .FontSize(11).Italic().FontColor(goldColor);

                        left.Item().PaddingTop(4).Text($"Địa chỉ: {SellerAddress}")
                            .FontSize(9).FontColor(grayText);
                        left.Item().Text($"MST: {SellerTaxCode}")
                            .FontSize(9).FontColor(grayText);
                        left.Item().Text($"Hotline: {SellerPhone} | Email: {SellerEmail}")
                            .FontSize(9).FontColor(grayText);
                    });

                    row.ConstantItem(200).AlignRight().Column(right =>
                    {
                        right.Item().Text(requireVat
                                ? "HÓA ĐƠN GIÁ TRỊ GIA TĂNG (NỘI BỘ)"
                                : "HÓA ĐƠN MUA HÀNG")
                            .FontSize(15).Bold().FontColor(primaryColor).AlignRight();

                        right.Item().Text($"Số: #{order.Orderid:D6}")
                            .FontSize(13).Bold().FontColor(goldColor).AlignRight();

                        right.Item().PaddingTop(4).Text($"Ngày lập: {(order.Orderdatetime ?? DateTime.Now):dd/MM/yyyy HH:mm}")
                            .FontSize(9).FontColor(grayText).AlignRight();

                        right.Item().Text($"Trạng thái: {TranslateStatus(order.Status)}")
                            .FontSize(9).FontColor(grayText).AlignRight();
                    });
                });

                col.Item().PaddingTop(8).LineHorizontal(2).LineColor(primaryColor);
            });
        }

        void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                // Buyer info
                col.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                    });

                    table.Cell().ColumnSpan(2).PaddingBottom(6)
                        .Text(requireVat ? "THÔNG TIN BÊN MUA / XUẤT HÓA ĐƠN" : "THÔNG TIN KHÁCH HÀNG")
                        .Bold().FontSize(12).FontColor(primaryColor);

                    table.Cell().Element(InfoCellStyle).Column(c =>
                    {
                        c.Item().Text(requireVat ? "Tên đơn vị / Công ty:" : "Tên khách hàng:").Bold().FontSize(10);
                        c.Item().Text(requireVat
                            ? (order.VatCompanyName ?? order.Customername ?? "N/A")
                            : (order.Customername ?? "N/A")).FontSize(11);
                    });

                    table.Cell().Element(InfoCellStyle).Column(c =>
                    {
                        c.Item().Text(requireVat ? "Mã số thuế:" : "Số điện thoại:").Bold().FontSize(10);
                        c.Item().Text(requireVat
                            ? (order.VatCompanyTaxCode ?? "N/A")
                            : (order.Customerphone ?? "N/A")).FontSize(11);
                    });

                    table.Cell().Element(InfoCellStyle).Column(c =>
                    {
                        c.Item().Text(requireVat ? "Email nhận hóa đơn:" : "Email:").Bold().FontSize(10);
                        c.Item().Text(requireVat
                            ? (order.VatInvoiceEmail ?? order.Customeremail ?? "N/A")
                            : (order.Customeremail ?? "N/A")).FontSize(11);
                    });

                    table.Cell().Element(InfoCellStyle).Column(c =>
                    {
                        c.Item().Text(requireVat ? "Địa chỉ đơn vị:" : "Địa chỉ giao hàng:").Bold().FontSize(10);
                        c.Item().Text(requireVat
                            ? (order.VatCompanyAddress ?? order.Customeraddress ?? "N/A")
                            : (order.Customeraddress ?? "N/A")).FontSize(11);
                    });
                });

                // Items
                col.Item().PaddingTop(18).Text("CHI TIẾT HÀNG HÓA / DỊCH VỤ")
                    .Bold().FontSize(12).FontColor(primaryColor);

                col.Item().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(30);
                        cols.RelativeColumn(4);
                        cols.ConstantColumn(70);
                        cols.ConstantColumn(50);
                        cols.ConstantColumn(90);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCellStyle).Text("STT").Bold().FontSize(10).AlignCenter();
                        header.Cell().Element(HeaderCellStyle).Text("Tên hàng hóa").Bold().FontSize(10);
                        header.Cell().Element(HeaderCellStyle).Text("Đơn giá").Bold().FontSize(10).AlignRight();
                        header.Cell().Element(HeaderCellStyle).Text("SL").Bold().FontSize(10).AlignCenter();
                        header.Cell().Element(HeaderCellStyle).Text("Thành tiền").Bold().FontSize(10).AlignRight();
                    });

                    var index = 1;
                    if (order.OrderDetails != null)
                    {
                        foreach (var item in order.OrderDetails)
                        {
                            var price = item.Product?.Price ?? 0;
                            var qty = item.Quantity ?? 0;
                            var amount = item.Amount ?? (price * qty);
                            var isEven = index % 2 == 0;

                            table.Cell().Element(c => BodyCellStyle(c, isEven)).Text(index.ToString()).FontSize(10).AlignCenter();
                            table.Cell().Element(c => BodyCellStyle(c, isEven)).Text(item.Product?.Productname ?? "N/A").FontSize(10);
                            table.Cell().Element(c => BodyCellStyle(c, isEven)).Text(FormatCurrency(price)).FontSize(10).AlignRight();
                            table.Cell().Element(c => BodyCellStyle(c, isEven)).Text(qty.ToString()).FontSize(10).AlignCenter();
                            table.Cell().Element(c => BodyCellStyle(c, isEven)).Text(FormatCurrency(amount)).FontSize(10).AlignRight();

                            index++;
                        }
                    }
                });

                // Summary
                col.Item().PaddingTop(16).AlignRight().Width(290).Column(sumCol =>
                {
                    sumCol.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.ConstantColumn(120);
                        });

                        t.Cell().Text("Tổng tiền hàng:").FontSize(11);
                        t.Cell().Text(FormatCurrency(subTotal)).FontSize(11).AlignRight();

                        if (discount > 0)
                        {
                            t.Cell().Text("Giảm giá / chiết khấu:").FontSize(11);
                            t.Cell().Text($"-{FormatCurrency(discount)}").FontSize(11).AlignRight().FontColor("#16A34A");
                        }

                        t.Cell().Text(requireVat ? "Giá tính thuế:" : "Thành tiền sau giảm giá:").FontSize(11);
                        t.Cell().Text(FormatCurrency(finalBaseAmount)).FontSize(11).AlignRight();

                        if (requireVat)
                        {
                            t.Cell().Text($"Thuế suất GTGT: {(vatRate * 100):N0}%").FontSize(11);
                            t.Cell().Text($"{(vatRate * 100):N0}%").FontSize(11).AlignRight();

                            t.Cell().Text("Tiền thuế GTGT:").FontSize(11);
                            t.Cell().Text(FormatCurrency(vatAmount)).FontSize(11).AlignRight();
                        }

                        t.Cell().ColumnSpan(2).PaddingTop(4).LineHorizontal(1).LineColor(primaryColor);

                        t.Cell().Text("TỔNG CỘNG THANH TOÁN:").Bold().FontSize(13).FontColor(primaryColor);
                        t.Cell().Text(FormatCurrency(requireVat ? finalPayableAmount : finalBaseAmount))
                            .Bold().FontSize(13).FontColor(primaryColor).AlignRight();
                    });
                });

                if (!string.IsNullOrWhiteSpace(order.Note))
                {
                    col.Item().PaddingTop(18).Column(noteCol =>
                    {
                        noteCol.Item().Text("Ghi chú:").Bold().FontSize(10).FontColor(grayText);
                        noteCol.Item().Text(order.Note).FontSize(10).FontColor(grayText).Italic();
                    });
                }

                if (requireVat)
                {
                    col.Item().PaddingTop(22).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                        });

                        table.Cell().AlignCenter().Column(c =>
                        {
                            c.Item().Text("NGƯỜI MUA HÀNG").Bold().FontSize(11);
                            c.Item().Text("(Ký, ghi rõ họ tên)").FontSize(9).Italic().FontColor(grayText);
                            c.Item().Height(60);
                            c.Item().Text(order.Customername ?? order.VatCompanyName ?? "").FontSize(10).SemiBold();
                        });

                        table.Cell().AlignCenter().Column(c =>
                        {
                            c.Item().Text("ĐẠI DIỆN BÊN BÁN").Bold().FontSize(11);
                            c.Item().Text("(Ký, ghi rõ họ tên)").FontSize(9).Italic().FontColor(grayText);
                            c.Item().Height(60);
                            c.Item().Text("Tết Gift").FontSize(10).SemiBold();
                        });
                    });

                    col.Item().PaddingTop(10).Text("Lưu ý: Đây là hóa đơn GTGT nội bộ phục vụ quy trình nghiệp vụ nội bộ, không thay thế hóa đơn điện tử do cơ quan thuế hoặc nhà cung cấp hóa đơn điện tử phát hành.")
                        .FontSize(8).Italic().FontColor(grayText);
                }
                else
                {
                    col.Item().PaddingTop(24).AlignCenter().Column(thankCol =>
                    {
                        thankCol.Item().Text("Cảm ơn quý khách đã tin tưởng và mua sắm tại TetGift!")
                            .FontSize(12).Bold().FontColor(primaryColor).AlignCenter();
                        thankCol.Item().PaddingTop(4).Text("Kính chúc quý khách Năm Mới An Khang, Thịnh Vượng!")
                            .FontSize(10).Italic().FontColor(grayText).AlignCenter();
                    });
                }
            });
        }

        void ComposeFooter(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().LineHorizontal(1).LineColor(primaryColor);
                col.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Text("www.tetgift.vn  |  support@tetgift.vn  |  1900 1234")
                        .FontSize(9).FontColor(grayText);
                    row.AutoItem().Text(c =>
                    {
                        c.Span("Trang ").FontSize(9).FontColor(grayText);
                        c.CurrentPageNumber().FontSize(9).FontColor(grayText);
                        c.Span(" / ").FontSize(9).FontColor(grayText);
                        c.TotalPages().FontSize(9).FontColor(grayText);
                    });
                });
            });
        }
    }

    private static IContainer HeaderCellStyle(IContainer container)
    {
        return container
            .Background("#A30D25")
            .Padding(6)
            .DefaultTextStyle(x => x.FontColor(Colors.White));
    }

    private static IContainer BodyCellStyle(IContainer container, bool isEven)
    {
        return container
            .Background(isEven ? "#FBF5E8" : Colors.White)
            .BorderBottom(1)
            .BorderColor("#E5E7EB")
            .Padding(6);
    }

    private static IContainer InfoCellStyle(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor("#E5E7EB")
            .Background("#FBF5E8")
            .Padding(8);
    }

    private static string FormatCurrency(decimal amount)
    {
        return $"{amount:N0} đ";
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
}