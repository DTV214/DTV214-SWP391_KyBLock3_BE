using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IAccountPromotionService
    {
        Task SaveToAccountAsync(AssignPromotionRequest req);
        Task<bool> UsePromotionAsync(int accountId, int promotionId);
    }
}