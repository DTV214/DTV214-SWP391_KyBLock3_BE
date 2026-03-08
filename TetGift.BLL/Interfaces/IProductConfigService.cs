using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IProductConfigService
    {
        Task<IEnumerable<ProductConfigDto>> GetAllAsync();
        Task<ProductConfigDto?> GetByIdAsync(int id);
        Task<IEnumerable<ProductConfigDto>> CreateAsync(CreateConfigRequest request);
        Task<IEnumerable<ProductConfigDto>> UpdateAsync(int configId, UpdateConfigRequest request);
        Task<IEnumerable<ProductConfigDto>> DeleteAsync(int id);
        Task<IEnumerable<ProductConfigDto>> HardDeleteAsync(int id);
    }
}
