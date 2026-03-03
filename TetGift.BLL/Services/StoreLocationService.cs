using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Interfaces;
using TetGift.DAL.Entities;

namespace TetGift.BLL.Services
{
    public class StoreLocationService : IStoreLocationService
    {
        private readonly IUnitOfWork _uow;

        public StoreLocationService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IEnumerable<StoreLocationDto>> GetActiveAsync()
        {
            var repo = _uow.GetRepository<StoreLocation>();

            var items = await repo.GetAllAsync(
                predicate: x => x.IsActive == true
            );

            return items.Select(MapToDto);
        }

        public async Task<IEnumerable<StoreLocationDto>> GetAllAsync()
        {
            var repo = _uow.GetRepository<StoreLocation>();
            var items = await repo.GetAllAsync();
            return items.Select(MapToDto);
        }

        public async Task<StoreLocationDto?> GetByIdAsync(int id)
        {
            if (id <= 0) return null;

            var repo = _uow.GetRepository<StoreLocation>();
            var entity = await repo.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<StoreLocationDto> CreateAsync(StoreLocationUpsertRequest req)
        {
            var repo = _uow.GetRepository<StoreLocation>();

            var entity = new StoreLocation
            {
                Name = req.Name,
                AddressLine = req.AddressLine,
                Latitude = req.Latitude,
                Longitude = req.Longitude,
                PhoneNumber = req.PhoneNumber,
                OpenHoursText = req.OpenHoursText,
                IsActive = req.IsActive
            };

            await repo.AddAsync(entity);
            await _uow.SaveAsync();

            return MapToDto(entity);
        }

        public async Task<StoreLocationDto> UpdateAsync(int id, StoreLocationUpsertRequest req)
        {
            if (id <= 0) throw new Exception("storeLocationId is required.");

            var repo = _uow.GetRepository<StoreLocation>();
            var entity = await repo.GetByIdAsync(id);
            if (entity == null) throw new Exception("Store location not found.");

            entity.Name = req.Name;
            entity.AddressLine = req.AddressLine;
            entity.Latitude = req.Latitude;
            entity.Longitude = req.Longitude;
            entity.PhoneNumber = req.PhoneNumber;
            entity.OpenHoursText = req.OpenHoursText;
            entity.IsActive = req.IsActive;

            await repo.UpdateAsync(entity);
            await _uow.SaveAsync();

            return MapToDto(entity);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (id <= 0) return false;

            var repo = _uow.GetRepository<StoreLocation>();
            var entity = await repo.GetByIdAsync(id);
            if (entity == null) return false;

            await repo.DeleteAsync(entity);
            await _uow.SaveAsync();
            return true;
        }

        private static StoreLocationDto MapToDto(StoreLocation x) => new()
        {
            StoreLocationId = x.StoreLocationId,
            Name = x.Name,
            AddressLine = x.AddressLine,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            PhoneNumber = x.PhoneNumber,
            OpenHoursText = x.OpenHoursText,
            IsActive = x.IsActive
        };
    }
}
