using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces;

public interface IFeedbackService
{
    Task<FeedbackResponseDto> AddFeedbackAsync(int orderId, int accountId, CreateFeedbackRequest request);
    Task<FeedbackResponseDto> UpdateFeedbackAsync(int feedbackId, int accountId, UpdateFeedbackRequest request);
    Task DeleteFeedbackAsync(int feedbackId, int accountId);
    Task<List<FeedbackResponseDto>> GetFeedbacksForProductAsync(int productId);
}
