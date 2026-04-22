namespace TetGift.BLL.Dtos
{
    public class QuotationDetailDto
    {
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

        public string? DesiredPriceNote { get; set; }
        public string? Note { get; set; }

        // VAT request info
        public bool RequireVatInvoice { get; set; }
        public string? VatCompanyName { get; set; }
        public string? VatCompanyTaxCode { get; set; }
        public string? VatCompanyAddress { get; set; }
        public string? VatInvoiceEmail { get; set; }
        public decimal VatRatePreview { get; set; }
        public decimal VatAmountPreview { get; set; }
        public decimal FinalPayablePreview { get; set; }

        public decimal TotalOriginal { get; set; }
        public decimal TotalSubtract { get; set; }
        public decimal TotalAdd { get; set; }
        public decimal TotalAfterDiscount { get; set; }
        public decimal TotalDiscountAmount { get; set; }

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
        public decimal UnitPrice { get; set; }
        public decimal OriginalLineTotal { get; set; }
        public decimal SubtractTotal { get; set; }
        public decimal AddTotal { get; set; }
        public decimal FinalLineTotal { get; set; }
        public List<QuotationFeeViewDto> Fees { get; set; } = new();
    }

    public class QuotationFeeViewDto
    {
        public int QuotationFeeId { get; set; }
        public short IsSubtracted { get; set; }
        public decimal Price { get; set; }
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
        public decimal? TotalPrice { get; set; }
        public int? Revision { get; set; }

        public bool RequireVatInvoice { get; set; }
        public decimal VatRatePreview { get; set; }
        public decimal VatAmountPreview { get; set; }
        public decimal FinalPayablePreview { get; set; }
    }
}