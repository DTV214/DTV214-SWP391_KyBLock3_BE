using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IProductConfigService
    {
        Task<IEnumerable<ProductConfigDto>> GetAllAsync();
        Task<ProductConfigDto?> GetByIdAsync(int id);
        Task CreateAsync(ProductConfigDto dto);
        Task UpdateAsync(ProductConfigDto dto);
        Task DeleteAsync(int id);
    }
}
