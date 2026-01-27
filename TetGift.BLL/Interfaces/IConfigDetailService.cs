using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces;

public interface IConfigDetailService
{
    Task<IEnumerable<ConfigDetailDto>> GetByConfigAsync(int configId);
    Task CreateAsync(ConfigDetailDto dto);
    Task UpdateAsync(ConfigDetailDto dto);
    Task DeleteAsync(int id);
}