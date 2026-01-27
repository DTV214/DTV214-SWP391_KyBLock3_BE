using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services
{
    public class ProductConfigService(IUnitOfWork uow) : IProductConfigService
    {
        private readonly IUnitOfWork _uow = uow;

        public async Task<IEnumerable<ProductConfigDto>> GetAllAsync()
        {
            var data = await _uow.GetRepository<ProductConfig>().FindAsync(pc => pc.Isdeleted == false);
            return data.Select(x => new ProductConfigDto
            {
                Configid = x.Configid,
                Configname = x.Configname,
                Suitablesuggestion = x.Suitablesuggestion,
                Totalunit = x.Totalunit,
                Imageurl = x.Imageurl
            });
        }

        public async Task<ProductConfigDto?> GetByIdAsync(int id)
        {
            var x = await _uow.GetRepository<ProductConfig>().GetByIdAsync(id);
            return x == null
                ? null
                : x.Isdeleted == true
                ? null
                : new ProductConfigDto
                {
                    Configid = x.Configid,
                    Configname = x.Configname,
                    Suitablesuggestion = x.Suitablesuggestion,
                    Totalunit = x.Totalunit,
                    Imageurl = x.Imageurl
                };
        }

        public async Task CreateAsync(ProductConfigDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Configname))
                throw new Exception("Tên cấu hình không được để trống.");

            var entity = new ProductConfig
            {
                Configname = dto.Configname,
                Suitablesuggestion = dto.Suitablesuggestion,
                Totalunit = dto.Totalunit,
                Imageurl = dto.Imageurl
            };
            await _uow.GetRepository<ProductConfig>().AddAsync(entity);
            await _uow.SaveAsync();
        }

        public async Task UpdateAsync(ProductConfigDto dto)
        {
            if (dto.Configname != null && string.IsNullOrWhiteSpace(dto.Configname))
                throw new Exception("Tên không được để trống.");

            if (dto.Totalunit.HasValue && dto.Totalunit <= 0)
                throw new Exception("Tổng đơn vị phải lớn hơn 0.");

            var repo = _uow.GetRepository<ProductConfig>();
            var entity = await repo.GetByIdAsync(dto.Configid);
            if (entity != null)
            {
                entity.Configname = dto.Configname;
                entity.Suitablesuggestion = dto.Suitablesuggestion;
                entity.Totalunit = dto.Totalunit;
                entity.Imageurl = dto.Imageurl;
                repo.Update(entity);
                await _uow.SaveAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            var repo = _uow.GetRepository<ProductConfig>();
            var entity = await repo.GetByIdAsync(id);
            if (entity != null)
            {
                entity.Isdeleted = true;
                repo.Update(entity);
                await _uow.SaveAsync();
            }
        }
    }
}
