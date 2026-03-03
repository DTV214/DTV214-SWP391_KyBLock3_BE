using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IStoreLocationService
    {
        Task<IEnumerable<StoreLocationDto>> GetActiveAsync();
        Task<IEnumerable<StoreLocationDto>> GetAllAsync();
        Task<StoreLocationDto?> GetByIdAsync(int id);

        Task<StoreLocationDto> CreateAsync(StoreLocationUpsertRequest req);
        Task<StoreLocationDto> UpdateAsync(int id, StoreLocationUpsertRequest req);
        Task<bool> DeleteAsync(int id);
    }
}
