using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IPromotionService
    {
        Task<PromotionResponseDto> CreateAsync(PromotionRequest req);
        Task<IEnumerable<PromotionResponseDto>> GetAllAsync();
        Task<IEnumerable<PromotionResponseDto>> GetAllAsync(bool isLimited);
        Task<PromotionResponseDto> GetByIdAsync(int id);
        Task<PromotionResponseDto> GetByCodeAsync(string code);
        Task UpdateAsync(int id, PromotionRequest req);
        Task DeleteAsync(int id);
    }
}
