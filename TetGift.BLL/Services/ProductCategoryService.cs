using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services;

public class ProductCategoryService(IUnitOfWork uow) : IProductCategoryService
{
    private readonly IUnitOfWork _uow = uow;

    public async Task<IEnumerable<ProductCategoryDto>> GetAllAsync()
    {
        var data = await _uow.GetRepository<ProductCategory>().FindAsync(c => c.Isdeleted == false);
        return data.Select(x => new ProductCategoryDto
        {
            Categoryid = x.Categoryid,
            Categoryname = x.Categoryname
        });
    }

    public async Task CreateAsync(ProductCategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Categoryname))
            throw new Exception("Tên danh mục không được để trống khi tạo mới.");

        var entity = new ProductCategory
        {
            Categoryname = dto.Categoryname,
            Isdeleted = false
        };

        await _uow.GetRepository<ProductCategory>().AddAsync(entity);
        await _uow.SaveAsync();
    }

    public async Task UpdateAsync(ProductCategoryDto dto)
    {
        var repo = _uow.GetRepository<ProductCategory>();
        var entity = await repo.GetByIdAsync(dto.Categoryid);

        if (entity != null && entity.Isdeleted == false)
        {
            if (dto.Categoryname != null)
            {
                if (string.IsNullOrWhiteSpace(dto.Categoryname))
                    throw new Exception("Tên danh mục không được để trống (blank) khi cập nhật.");

                entity.Categoryname = dto.Categoryname;
            }

            repo.Update(entity);
            await _uow.SaveAsync();
        }
    }

    public async Task DeleteAsync(int id)
    {
        var repo = _uow.GetRepository<ProductCategory>();
        var entity = await repo.GetByIdAsync(id);
        if (entity != null)
        {
            entity.Isdeleted = true;
            repo.Update(entity);
            await _uow.SaveAsync();
        }
    }
}