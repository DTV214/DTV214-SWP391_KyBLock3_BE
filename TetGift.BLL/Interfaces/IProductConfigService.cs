using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IProductConfigService
    {
        Task<IEnumerable<ProductConfigDto>> GetAllAsync();
        Task<ProductConfigDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateConfigRequest request);
        Task UpdateAsync(int configId, UpdateConfigRequest request);
        Task DeleteAsync(int id);
    }
}
