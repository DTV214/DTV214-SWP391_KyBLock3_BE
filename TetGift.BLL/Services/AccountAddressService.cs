using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services
{
    public class AccountAddressService : IAccountAddressService
    {
        private readonly IUnitOfWork _uow;

        public AccountAddressService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IEnumerable<AccountAddressDto>> GetMyAddressesAsync(int accountId)
        {
            if (accountId <= 0) throw new Exception("accountId is required.");

            var repo = _uow.GetRepository<AccountAddress>();
            var items = await repo.GetAllAsync(x => x.AccountId == accountId && x.IsActive == true);
            return items.Select(MapToDto);
        }

        public async Task<AccountAddressDto?> GetMyDefaultAsync(int accountId)
        {
            if (accountId <= 0) return null;

            var repo = _uow.GetRepository<AccountAddress>();

            var list = await repo.FindAsync(x => x.AccountId == accountId && x.IsDefault == true && x.IsActive == true);
            var entity = list.FirstOrDefault();

            return entity == null ? null : MapToDto(entity);
        }

        public async Task<AccountAddressDto> CreateMyAddressAsync(int accountId, AccountAddressUpsertRequest req)
        {
            if (accountId <= 0) throw new Exception("accountId is required.");

            var repo = _uow.GetRepository<AccountAddress>();

            if (req.IsDefault)
            {
                await UnsetDefaultAsync(accountId);
            }

            var entity = new AccountAddress
            {
                AccountId = accountId,
                Label = req.Label,
                AddressLine = req.AddressLine,
                Latitude = req.Latitude,
                Longitude = req.Longitude,
                IsDefault = req.IsDefault,
                IsActive = req.IsActive
            };

            await repo.AddAsync(entity);
            await _uow.SaveAsync();

            return MapToDto(entity);
        }

        public async Task<AccountAddressDto> UpdateMyAddressAsync(int accountId, int addressId, AccountAddressUpsertRequest req)
        {
            if (accountId <= 0) throw new Exception("accountId is required.");
            if (addressId <= 0) throw new Exception("addressId is required.");

            var repo = _uow.GetRepository<AccountAddress>();
            var entity = await repo.GetByIdAsync(addressId);
            if (entity == null || entity.AccountId != accountId) throw new Exception("Address not found.");

            if (req.IsDefault && entity.IsDefault == false)
            {
                await UnsetDefaultAsync(accountId);
            }

            entity.Label = req.Label;
            entity.AddressLine = req.AddressLine;
            entity.Latitude = req.Latitude;
            entity.Longitude = req.Longitude;
            entity.IsDefault = req.IsDefault;
            entity.IsActive = req.IsActive;

            await repo.UpdateAsync(entity);
            await _uow.SaveAsync();

            return MapToDto(entity);
        }

        public async Task<bool> DeleteMyAddressAsync(int accountId, int addressId)
        {
            if (accountId <= 0) return false;
            if (addressId <= 0) return false;

            var repo = _uow.GetRepository<AccountAddress>();
            var entity = await repo.GetByIdAsync(addressId);
            if (entity == null || entity.AccountId != accountId) return false;

            await repo.DeleteAsync(entity);
            await _uow.SaveAsync();
            return true;
        }

        public async Task<bool> SetMyDefaultAsync(int accountId, int addressId)
        {
            if (accountId <= 0) return false;
            if (addressId <= 0) return false;

            var repo = _uow.GetRepository<AccountAddress>();
            var entity = await repo.GetByIdAsync(addressId);
            if (entity == null || entity.AccountId != accountId) return false;

            await UnsetDefaultAsync(accountId);

            entity.IsDefault = true;
            await repo.UpdateAsync(entity);
            await _uow.SaveAsync();
            return true;
        }

        private async Task UnsetDefaultAsync(int accountId)
        {
            var repo = _uow.GetRepository<AccountAddress>();
            var defaults = await repo.FindAsync(x => x.AccountId == accountId && x.IsDefault == true);
            var list = await repo.GetAllAsync(x => x.AccountId == accountId && x.IsDefault == true);

            foreach (var item in list)
            {
                item.IsDefault = false;
                await repo.UpdateAsync(item);
            }
        }

        private static AccountAddressDto MapToDto(AccountAddress x) => new()
        {
            AccountAddressId = x.AccountAddressId,
            AccountId = x.AccountId,
            Label = x.Label,
            AddressLine = x.AddressLine,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            IsDefault = x.IsDefault,
            IsActive = x.IsActive
        };
    }
}
