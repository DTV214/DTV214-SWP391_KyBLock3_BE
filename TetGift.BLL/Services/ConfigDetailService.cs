using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services;

public class ConfigDetailService(IUnitOfWork uow) : IConfigDetailService
{
    private readonly IUnitOfWork _uow = uow;

    // Config and Category Validation
    private async Task ValidateForeignKeys(int? configId, int? categoryId)
    {
        if (configId.HasValue)
        {
            var config = await _uow.GetRepository<ProductConfig>().FindAsync(
                pc => pc.Configid == configId.Value && pc.Isdeleted == false
                );
            if (!config.Any())
                throw new Exception($"ConfigId {configId} không tồn tại hoặc đã bị xóa.");
        }

        if (categoryId.HasValue)
        {
            var category = await _uow.GetRepository<ProductCategory>().FindAsync(
                pc => pc.Categoryid == categoryId.Value && pc.Isdeleted == false
                );
            if (!category.Any())
                throw new Exception($"CategoryId {categoryId} không tồn tại hoặc đã bị xóa.");
        }
    }

    public async Task CreateAsync(ConfigDetailDto dto)
    {
        // 1. Check null/giá trị Quantity
        if (dto.Quantity == null || dto.Quantity <= 0)
            throw new Exception("Số lượng phải là số nguyên dương.");

        if ((string.IsNullOrEmpty(dto.Configid.ToString())))
            throw new Exception("Config Id không được thiếu.");

        // 2. Check tồn tại của Config và Category
        await ValidateForeignKeys(dto.Configid, dto.Categoryid);

        var entity = new ConfigDetail
        {
            Configid = dto.Configid,
            Categoryid = dto.Categoryid,
            Quantity = dto.Quantity
        };

        await _uow.GetRepository<ConfigDetail>().AddAsync(entity);
        await _uow.SaveAsync();
    }

    public async Task UpdateAsync(ConfigDetailDto dto)
    {
        var repo = _uow.GetRepository<ConfigDetail>();
        var entity = await repo.GetByIdAsync(dto.Configdetailid);

        if (entity != null)
        {
            if (string.IsNullOrEmpty(dto.Configid.ToString()))
            {
                if (dto.Quantity <= 0) throw new Exception("Số lượng cập nhật phải lớn hơn 0.");
                entity.Quantity = dto.Quantity;
            }

            if (string.IsNullOrEmpty(dto.Configid.ToString()))
            {
                await ValidateForeignKeys(null, dto.Categoryid);
                entity.Categoryid = dto.Categoryid;
            }

            repo.Update(entity);
            await _uow.SaveAsync();
        }
    }

    public async Task<IEnumerable<ConfigDetailDto>> GetByConfigAsync(int configId)
    {
        var data = await _uow.GetRepository<ConfigDetail>().FindAsync(
            cd => cd.Config.Configid == configId && cd.Config.Isdeleted == false
            );
        return data.Select(x => new ConfigDetailDto
        {
            Configdetailid = x.Configdetailid,
            Configid = (int)x.Configid,
            Categoryid = (int)x.Categoryid,
            Quantity = (int)x.Quantity
        });
    }

    public async Task DeleteAsync(int id)
    {
        var repo = _uow.GetRepository<ConfigDetail>();
        var entity = await repo.GetByIdAsync(id);
        if (entity != null)
        {
            repo.Delete(entity);
            await _uow.SaveAsync();
        }
    }
}