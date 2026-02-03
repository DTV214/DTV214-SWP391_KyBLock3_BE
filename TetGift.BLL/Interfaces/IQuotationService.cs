using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IQuotationService
    {
        // CUSTOMER - flow 1
        Task<QuotationSimpleDto> CreateManualAsync(QuotationCreateManualRequest req);
        Task<QuotationSimpleDto> UpdateDraftAsync(int quotationId, QuotationUpdateDraftRequest req);
        Task SubmitAsync(int quotationId, QuotationSubmitRequest req);
        Task CustomerAcceptAsync(int quotationId, CustomerDecisionRequest req);
        Task CustomerRejectAsync(int quotationId, CustomerDecisionRequest req);
        Task<List<QuotationListItemDto>> GetCustomerQuotationsAsync(int accountId, string? status = null);
        Task<QuotationDetailDto> GetCustomerQuotationDetailAsync(int quotationId, int accountId);

        // STAFF
        Task StartReviewAsync(int quotationId, int staffAccountId);
        //Task ProposePriceAsync(int quotationId, StaffProposePriceRequest req);
        Task ProposeItemDiscountsAsync(int quotationId, StaffProposeItemDiscountRequest req);
        Task SendToAdminAsync(int quotationId, int staffAccountId, string? message);
        Task<List<QuotationListItemDto>> GetStaffQuotationsAsync(string? status = null);
        Task<QuotationDetailDto> GetStaffQuotationDetailAsync(int quotationId);
        //Task StaffReviewFeesAsync(int quotationId, StaffReviewFeesRequest req);
        Task CreateQuotationFeeAsync(int quotationId, StaffCreateFeeRequest req);
        Task UpdateQuotationFeeAsync(int quotationId, StaffUpdateFeeRequest req);
        Task DeleteQuotationFeeAsync(int quotationId, int quotationFeeId, int staffAccountId); 
        Task<List<QuotationFeeOnItemViewDto>> GetFeesByQuotationItemAsync(int quotationId, int quotationItemId);

        // ADMIN
        Task AdminApproveAsync(int quotationId, AdminDecisionRequest req);
        Task AdminRejectAsync(int quotationId, AdminDecisionRequest req);
        Task<List<QuotationListItemDto>> GetAdminQuotationsAsync(string? status = null);
        Task<QuotationDetailDto> GetAdminQuotationDetailAsync(int quotationId);

        // CUSTOMER - flow 2
        Task<RecommendPreviewDto> RequestRecommendAsync(QuotationRecommendRequest req);
        Task CustomerConfirmRecommendAsync(int quotationId, QuotationRecommendConfirmRequest req);
    }
}
