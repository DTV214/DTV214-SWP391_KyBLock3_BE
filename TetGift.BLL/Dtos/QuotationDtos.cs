namespace TetGift.BLL.Dtos
{
    public class QuotationItemUpsertDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    //Flow 1: luồng bình thường
    public class QuotationCreateManualRequest
    {
        public int AccountId { get; set; }
        public string? Company { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? DesiredPriceNote { get; set; }
        public string? Note { get; set; }
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
        public List<QuotationItemUpsertDto>? Items { get; set; }
    }

    public class QuotationSubmitRequest
    {
        public int AccountId { get; set; }
    }

    // Staff propose price (and optionally fees later)
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
        public string? Message { get; set; } // reason / desired change
    }

    public class StaffDiscountLineDto
    {
        public int QuotationItemId { get; set; }
        public decimal DiscountPercent { get; set; } // 0..100
    }

    public class StaffFeeInputDto
    {
        public short IsSubtracted { get; set; } // 0 = trừ, 1 = cộng
        public decimal Price { get; set; }      // số tiền điều chỉnh (delta)
        public string? Description { get; set; } // "Giảm 10%", "Phí ship", ...
    }

    public class StaffReviewFeesLineDto
    {
        public int QuotationItemId { get; set; }
        public List<StaffFeeInputDto> Fees { get; set; } = new();
    }

    public class StaffReviewFeesRequest
    {
        // sẽ override từ JWT trong controller
        public int StaffAccountId { get; set; }
        public List<StaffReviewFeesLineDto> Lines { get; set; } = new();
        public string? Message { get; set; }
    }

    public class StaffProposeItemDiscountRequest
    {
        public int StaffAccountId { get; set; }
        public List<StaffDiscountLineDto> Lines { get; set; } = new();
        public string? Message { get; set; }
    }

    //Flow 2: tự reccommend
    public class RecommendCategoryInputDto
    {
        public int CategoryId { get; set; }
        public int? Quantity { get; set; }
        public string? Note { get; set; }
    }

    public class QuotationRecommendRequest
    {
        public int AccountId { get; set; }
        public decimal Budget { get; set; }
        public string? Note { get; set; }
        public List<RecommendCategoryInputDto> Categories { get; set; } = new();
    }

    public class QuotationRecommendConfirmRequest
    {
        public int AccountId { get; set; }
        public bool AutoCreateOrder { get; set; } = true; // nếu đồng ý thì tạo order API riêng
    }

    // response
    public class QuotationSimpleDto
    {
        public int QuotationId { get; set; }
        public string? Status { get; set; }
        public string? QuotationType { get; set; }
        public decimal? DesiredBudget { get; set; }
        public decimal? TotalPrice { get; set; }
        public int? Revision { get; set; }
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
