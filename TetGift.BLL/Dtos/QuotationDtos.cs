namespace TetGift.BLL.Dtos
{
    public class QuotationItemUpsertDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    // Flow 1: manual quotation
    public class QuotationCreateManualRequest
    {
        public int AccountId { get; set; }
        public string? Company { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? DesiredPriceNote { get; set; }
        public string? Note { get; set; }

        public bool RequireVatInvoice { get; set; } = false;
        public string? VatCompanyName { get; set; }
        public string? VatCompanyTaxCode { get; set; }
        public string? VatCompanyAddress { get; set; }
        public string? VatInvoiceEmail { get; set; }

        public List<QuotationItemUpsertDto> Items { get; set; } = new();
    }

    public class QuotationUpdateDraftRequest
    {
        public int AccountId { get; set; }
        public string? Company { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? DesiredPriceNote { get; set; }
        public string? Note { get; set; }

        public bool? RequireVatInvoice { get; set; }
        public string? VatCompanyName { get; set; }
        public string? VatCompanyTaxCode { get; set; }
        public string? VatCompanyAddress { get; set; }
        public string? VatInvoiceEmail { get; set; }

        public List<QuotationItemUpsertDto>? Items { get; set; }
    }

    public class QuotationSubmitRequest
    {
        public int AccountId { get; set; }
    }

    public class StaffProposePriceRequest
    {
        public int StaffAccountId { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Message { get; set; }
    }

    public class AdminDecisionRequest
    {
        public int AdminAccountId { get; set; }
        public string? Message { get; set; }
    }

    public class CustomerDecisionRequest
    {
        public int AccountId { get; set; }
        public string? Message { get; set; }
    }

    public class StaffDiscountLineDto
    {
        public int QuotationItemId { get; set; }
        public decimal DiscountPercent { get; set; }
    }

    public class StaffCreateFeeRequest
    {
        public int StaffAccountId { get; set; }
        public int QuotationItemId { get; set; }
        public short IsSubtracted { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
    }

    public class StaffUpdateFeeRequest
    {
        public int StaffAccountId { get; set; }
        public int QuotationFeeId { get; set; }
        public short IsSubtracted { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
    }

    public class StaffFeeInputDto
    {
        public int? QuotationFeeId { get; set; }
        public short IsSubtracted { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class StaffReviewFeesLineDto
    {
        public int QuotationItemId { get; set; }
        public List<StaffFeeInputDto> Fees { get; set; } = new();
    }

    public class StaffReviewFeesRequest
    {
        public int StaffAccountId { get; set; }
        public List<StaffReviewFeesLineDto> Lines { get; set; } = new();
        public string? Message { get; set; }
    }

    public class QuotationFeeOnItemViewDto
    {
        public int QuotationFeeId { get; set; }
        public int QuotationItemId { get; set; }
        public short IsSubtracted { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
    }

    public class StaffProposeItemDiscountRequest
    {
        public int StaffAccountId { get; set; }
        public List<StaffDiscountLineDto> Lines { get; set; } = new();
        public string? Message { get; set; }
    }

    // Flow 2: recommend quotation
    public class RecommendCategoryInputDto
    {
        public int CategoryId { get; set; }
        public int? Quantity { get; set; }
        public string? Note { get; set; }
    }

    public class QuotationRecommendRequest
    {
        public int AccountId { get; set; }

        public string? Company { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }

        public decimal Budget { get; set; }
        public string? Note { get; set; }

        public bool RequireVatInvoice { get; set; } = false;
        public string? VatCompanyName { get; set; }
        public string? VatCompanyTaxCode { get; set; }
        public string? VatCompanyAddress { get; set; }
        public string? VatInvoiceEmail { get; set; }

        public List<RecommendCategoryInputDto> Categories { get; set; } = new();
    }

    public class QuotationRecommendConfirmRequest
    {
        public int AccountId { get; set; }
        public bool AutoCreateOrder { get; set; } = true;
    }

    public class QuotationSimpleDto
    {
        public int QuotationId { get; set; }
        public string? Status { get; set; }
        public string? QuotationType { get; set; }
        public decimal? DesiredBudget { get; set; }
        public decimal? TotalPrice { get; set; }
        public int? Revision { get; set; }

        public bool RequireVatInvoice { get; set; }
        public decimal VatRatePreview { get; set; }
        public decimal VatAmountPreview { get; set; }
        public decimal FinalPayablePreview { get; set; }
    }

    public class RecommendPreviewItemDto
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal? Price { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal => (Price ?? 0) * Quantity;
    }

    public class RecommendPreviewDto
    {
        public QuotationSimpleDto Quotation { get; set; } = new();
        public decimal Budget { get; set; }
        public decimal EstimatedTotal { get; set; }
        public List<RecommendPreviewItemDto> Items { get; set; } = new();
    }
}