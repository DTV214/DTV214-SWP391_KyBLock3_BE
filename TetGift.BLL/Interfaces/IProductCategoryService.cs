using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces;

public interface IProductCategoryService
{
    Task<IEnumerable<ProductCategoryDto>> GetAllAsync();
    Task CreateAsync(ProductCategoryDto dto);
    Task UpdateAsync(ProductCategoryDto dto);
    Task DeleteAsync(int id);
}