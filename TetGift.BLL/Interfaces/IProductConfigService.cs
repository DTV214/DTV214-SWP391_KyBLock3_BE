using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IProductConfigService
    {
        Task<IEnumerable<ProductConfigDto>> GetAllAsync();
        Task<ProductConfigDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(ProductConfigDto dto);
        Task<int> CreateWithDetailsAsync(string configname, string? description, Dictionary<int, int> categoryQuantities);
        Task UpdateAsync(ProductConfigDto dto);
        Task UpdateWithDetailsAsync(int configId, string configname, string? description, Dictionary<int, int> categoryQuantities);
        Task DeleteAsync(int id);
    }
}
