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

    public InvoiceService(IUnitOfWork uow)
    {
        _uow = uow;
        // QuestPDF community license (free for open source / internal use)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(int orderId, int? accountId)
    {
        var orderRepo = _uow.GetRepository<Order>();

        // Build query
        IQueryable<Order> query;
        if (accountId.HasValue)
        {
            query = orderRepo.Entities
                .Where(o => o.Orderid == orderId && o.Accountid == accountId.Value)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.Promotion)
                .Include(o => o.Account);
        }
        else
        {
            query = orderRepo.Entities
                .Where(o => o.Orderid == orderId)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
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
        // Colors
        var primaryColor = "#690000";   // Đỏ Tết (Primary)
        var goldColor = "#D4AF37";      // Vàng đồng
        var bgColor = "#FBF5E8";        // Kem nhạt (Background)
        var grayText = "#4B5563";       // Xám đậm trang trọng

        // Calculate totals
        decimal subTotal = 0;
        if (order.OrderDetails != null)
        {
            foreach (var d in order.OrderDetails)
            {
                subTotal += d.Amount ?? (d.Product?.Price ?? 0) * (d.Quantity ?? 0);
            }
        }

        decimal discount = 0;
        if (order.Promotion != null)
        {
            if (order.Promotion.IsPercentage ?? false)
                discount = subTotal * ((order.Promotion.Discountvalue ?? 0) / 100);
            else
                discount = order.Promotion.Discountvalue ?? 0;

            if (order.Promotion.MaxDiscountPrice.HasValue && discount > order.Promotion.MaxDiscountPrice.Value)
                discount = order.Promotion.MaxDiscountPrice.Value;
        }
        decimal finalPrice = order.Totalprice ?? (subTotal - discount);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginLeft(40);
                page.MarginRight(40);
                page.MarginTop(40);
                page.MarginBottom(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Helvetica"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(content => ComposeContent(content, order, subTotal, discount, finalPrice));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();

        void ComposeHeader(IContainer container)
        {
            container.PaddingBottom(16).Column(col =>
            {
                col.Item().Row(row =>
                {
                    // Left: Shop info
                    row.RelativeItem().Column(shopCol =>
                    {
                        shopCol.Item().Text("TetGift")
                            .FontSize(24).Bold().FontColor(primaryColor);
                        shopCol.Item().Text("Quà Tặng Tết Nguyên Đán Cao Cấp")
                            .FontSize(12).FontColor(goldColor).Italic();
                        shopCol.Item().PaddingTop(4).Text("Hotline: 1900 1234  |  Email: support@tetgift.vn")
                            .FontSize(10).FontColor(grayText);
                        shopCol.Item().Text("Website: www.tetgift.vn")
                            .FontSize(10).FontColor(grayText);
                    });

                    // Right: Invoice title
                    row.ConstantItem(160).AlignRight().Column(invoiceCol =>
                    {
                        invoiceCol.Item().Text("HÓA ĐƠN MUA HÀNG")
                            .FontSize(16).Bold().FontColor(primaryColor).AlignRight();
                        invoiceCol.Item().Text($"#{order.Orderid:D6}")
                            .FontSize(14).Bold().FontColor(goldColor).AlignRight();
                        invoiceCol.Item().PaddingTop(4).Text($"Ngày: {(order.Orderdatetime ?? DateTime.Now):dd/MM/yyyy}")
                            .FontSize(10).FontColor(grayText).AlignRight();
                        invoiceCol.Item().Text($"Trạng thái: {TranslateStatus(order.Status)}")
                            .FontSize(10).FontColor(grayText).AlignRight();
                    });
                });

                col.Item().PaddingTop(8).LineHorizontal(2).LineColor(primaryColor);
            });
        }

        void ComposeContent(IContainer container, Order order, decimal subTotal, decimal discount, decimal finalPrice)
        {
            container.Column(col =>
            {
                // Customer info
                col.Item().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                    });

                    table.Cell().ColumnSpan(2).PaddingBottom(6)
                        .Text("THÔNG TIN KHÁCH HÀNG").Bold().FontSize(12).FontColor(primaryColor);

                    table.Cell().Element(InfoCellStyle).Column(c =>
                    {
                        c.Item().Text("Tên khách hàng:").Bold().FontSize(10);
                        c.Item().Text(order.Customername ?? "N/A").FontSize(11);
                    });
                    table.Cell().Element(InfoCellStyle).Column(c =>
                    {
                        c.Item().Text("Số điện thoại:").Bold().FontSize(10);
                        c.Item().Text(order.Customerphone ?? "N/A").FontSize(11);
                    });
                    table.Cell().Element(InfoCellStyle).Column(c =>
                    {
                        c.Item().Text("Email:").Bold().FontSize(10);
                        c.Item().Text(order.Customeremail ?? "N/A").FontSize(11);
                    });
                    table.Cell().Element(InfoCellStyle).Column(c =>
                    {
                        c.Item().Text("Địa chỉ giao hàng:").Bold().FontSize(10);
                        c.Item().Text(order.Customeraddress ?? "N/A").FontSize(11);
                    });
                });

                // Order items table
                col.Item().PaddingTop(20).Text("CHI TIẾT ĐƠN HÀNG").Bold().FontSize(12).FontColor(primaryColor);
                col.Item().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(30);    // STT
                        cols.RelativeColumn(4);     // Tên SP
                        cols.ConstantColumn(70);    // Đơn giá
                        cols.ConstantColumn(50);    // SL
                        cols.ConstantColumn(85);    // Thành tiền
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCellStyle).Text("STT").Bold().FontSize(10).AlignCenter();
                        header.Cell().Element(HeaderCellStyle).Text("Sản phẩm").Bold().FontSize(10);
                        header.Cell().Element(HeaderCellStyle).Text("Đơn giá").Bold().FontSize(10).AlignRight();
                        header.Cell().Element(HeaderCellStyle).Text("SL").Bold().FontSize(10).AlignCenter();
                        header.Cell().Element(HeaderCellStyle).Text("Thành tiền").Bold().FontSize(10).AlignRight();
                    });

                    // Rows
                    int index = 1;
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
                col.Item().PaddingTop(16).AlignRight().Width(260).Column(sumCol =>
                {
                    sumCol.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.ConstantColumn(110);
                        });

                        t.Cell().Text("Tổng tiền hàng:").FontSize(11);
                        t.Cell().Text(FormatCurrency(subTotal)).FontSize(11).AlignRight();

                        if (discount > 0)
                        {
                            t.Cell().Text($"Giảm giá ({order.Promotion?.Code}):").FontSize(11).FontColor(grayText);
                            t.Cell().Text($"-{FormatCurrency(discount)}").FontSize(11).FontColor("#16A34A").AlignRight();
                        }

                        t.Cell().ColumnSpan(2).PaddingTop(4).LineHorizontal(1).LineColor(primaryColor);

                        t.Cell().Text("TỔNG THANH TOÁN:").Bold().FontSize(13).FontColor(primaryColor);
                        t.Cell().Text(FormatCurrency(finalPrice)).Bold().FontSize(13).FontColor(primaryColor).AlignRight();
                    });
                });

                // Note
                if (!string.IsNullOrWhiteSpace(order.Note))
                {
                    col.Item().PaddingTop(20).Column(noteCol =>
                    {
                        noteCol.Item().Text("Ghi chú:").Bold().FontSize(10).FontColor(grayText);
                        noteCol.Item().Text(order.Note).FontSize(10).FontColor(grayText).Italic();
                    });
                }

                // Thank you message
                col.Item().PaddingTop(30).AlignCenter().Column(thankCol =>
                {
                    thankCol.Item().Text("Cảm ơn quý khách đã tin tưởng và mua sắm tại TetGift!")
                        .FontSize(12).Bold().FontColor(primaryColor).AlignCenter();
                    thankCol.Item().PaddingTop(4).Text("Kính chúc quý khách Năm Mới An Khang, Thịnh Vượng!")
                        .FontSize(10).Italic().FontColor(grayText).AlignCenter();
                });

                // Locations
                col.Item().PaddingTop(20).Column(locCol =>
                {
                    locCol.Item().PaddingBottom(4).Text("HỆ THỐNG CỬA HÀNG:").FontSize(10).Bold().FontColor(primaryColor);
                    locCol.Item().Text("CN1: TetGift - HCM (Quận 1) - 15 Lê Lợi, Bến Nghé, Quận 1, TP.HCM").FontSize(9).FontColor(grayText);
                    locCol.Item().Text("CN2: TetGift - HCM (Thủ Đức) - Khu Công Nghệ Cao, TP. Thủ Đức, TP.HCM").FontSize(9).FontColor(grayText);
                    locCol.Item().Text("CN3: TetGift - Hà Nội (Hoàn Kiếm) - 25 Tràng Tiền, Hoàn Kiếm, Hà Nội").FontSize(9).FontColor(grayText);
                    locCol.Item().Text("CN4: TetGift - Đà Nẵng (Hải Châu) - 230 Trần Phú, Hải Châu, Đà Nẵng").FontSize(9).FontColor(grayText);
                    locCol.Item().Text("CN5: TetGift - Bình Tân - 120 Lê Văn Quới, Bình Tân, Hồ Chí Minh").FontSize(9).FontColor(grayText);
                });
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
