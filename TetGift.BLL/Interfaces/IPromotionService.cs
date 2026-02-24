using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IPromotionService
    {
        Task<PromotionResponseDto> CreateAsync(CreatePromotionRequest req);
        Task<IEnumerable<PromotionResponseDto>> GetAllAsync();
        // --- THÊM MỚI ---
        Task<PromotionResponseDto> GetByIdAsync(int id);
        Task<PromotionResponseDto> GetByCodeAsync(string code);
        Task UpdateAsync(int id, UpdatePromotionRequest req);
        Task DeleteAsync(int id);
    }
}
