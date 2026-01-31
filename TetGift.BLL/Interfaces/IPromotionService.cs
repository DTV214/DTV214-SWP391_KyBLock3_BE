using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IPromotionService
    {
        Task<PromotionResponseDto> CreateAsync(CreatePromotionRequest req);
        Task<IEnumerable<PromotionResponseDto>> GetAllAsync();
        // --- THÊM MỚI ---
        Task<PromotionResponseDto> GetByIdAsync(int id);
        Task UpdateAsync(int id, UpdatePromotionRequest req);
        Task DeleteAsync(int id);
    }
}
