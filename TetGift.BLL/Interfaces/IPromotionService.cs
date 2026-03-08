using TetGift.BLL.Dtos;
using TetGift.DAL.Entities;

namespace TetGift.BLL.Interfaces
{
    public interface IPromotionService
    {
        Task<PromotionResponseDto> CreateAsync(PromotionRequest req);
        Task<IEnumerable<PromotionResponseDto>> GetAllAsync();
        Task<IEnumerable<PromotionResponseDto>> GetAllAsync(bool isLimited);
        Task<IEnumerable<PromotionResponseDto>> GetAllAsync(bool isLimited, int accountId);
        Task<PromotionResponseDto> GetByIdAsync(int id);
        Task<PromotionResponseDto> GetByCodeAsync(string code);
        Task<Promotion> GetCodeAsync(string code);
        Task<IEnumerable<PromotionResponseDto>> GetByAccount(int accountId);
        Task UpdateAsync(int id, PromotionRequest req);
        Task DeleteAsync(int id);
    }
}
