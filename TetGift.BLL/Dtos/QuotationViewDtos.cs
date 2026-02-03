namespace TetGift.BLL.Dtos
{
    public class QuotationDetailDto
    {
        // Header
        public int QuotationId { get; set; }
        public int? AccountId { get; set; }
        public int? OrderId { get; set; }
        public string? Status { get; set; }
        public string? QuotationType { get; set; }
        public int? Revision { get; set; }

        public DateTime? RequestDate { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? StaffReviewedAt { get; set; }
        public DateTime? AdminReviewedAt { get; set; }
        public DateTime? CustomerRespondedAt { get; set; }

        public int? StaffReviewerId { get; set; }
        public int? AdminReviewerId { get; set; }

        public string? Company { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }

        public string? DesiredPriceNote { get; set; } // user mong muốn giá
        public string? Note { get; set; }             // ghi chú chung

        // Totals
        public decimal TotalOriginal { get; set; }    // tổng giá gốc (sum QuotationItem.Price)
        public decimal TotalSubtract { get; set; }
        public decimal TotalAdd { get; set; }
        public decimal TotalAfterDiscount { get; set; } // tổng sau giảm (sum QuotationFee.Price)
        public decimal TotalDiscountAmount { get; set; } // tổng giảm = original - after

        // Lines + messages
        public List<QuotationLineDto> Lines { get; set; } = new();
        public List<QuotationMessageDto> Messages { get; set; } = new();
    }

    public class QuotationLineDto
    {
        public int QuotationItemId { get; set; }
        public int ProductId { get; set; }
        public string? Sku { get; set; }
        public string? ProductName { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }          // Product.Price
        public decimal OriginalLineTotal { get; set; }  // QuotationItem.Price (or fallback unit*qty)

        public decimal SubtractTotal { get; set; }
        public decimal AddTotal { get; set; }
        public decimal FinalLineTotal { get; set; }
        public List<QuotationFeeViewDto> Fees { get; set; } = new();
        //public decimal DiscountAmount => Math.Max(0, OriginalLineTotal - AfterDiscountLineTotal);
    }
    public class QuotationFeeViewDto
    {
        public int QuotationFeeId { get; set; }
        public short IsSubtracted { get; set; } // 0 trừ, 1 cộng
        public decimal Price { get; set; }      // delta
        public string? Description { get; set; }
    }


    public class QuotationMessageDto
    {
        public int QuotationMessageId { get; set; }
        public string? FromRole { get; set; }
        public int? FromAccountId { get; set; }
        public string? ToRole { get; set; }
        public string? ActionType { get; set; }
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? MetaJson { get; set; }
    }

    public class QuotationListItemDto
    {
        public int QuotationId { get; set; }
        public string? Status { get; set; }
        public DateTime? RequestDate { get; set; }
        public string? Company { get; set; }
        public decimal? TotalPrice { get; set; } // total after discount nếu staff đã set
        public int? Revision { get; set; }
    }
}
