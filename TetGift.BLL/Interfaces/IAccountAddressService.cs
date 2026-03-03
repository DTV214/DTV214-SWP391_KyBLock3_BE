using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IAccountAddressService
    {
        Task<IEnumerable<AccountAddressDto>> GetMyAddressesAsync(int accountId);
        Task<AccountAddressDto?> GetMyDefaultAsync(int accountId);
        Task<AccountAddressDto> CreateMyAddressAsync(int accountId, AccountAddressUpsertRequest req);
        Task<AccountAddressDto> UpdateMyAddressAsync(int accountId, int addressId, AccountAddressUpsertRequest req);
        Task<bool> DeleteMyAddressAsync(int accountId, int addressId);
        Task<bool> SetMyDefaultAsync(int accountId, int addressId);
    }
}
