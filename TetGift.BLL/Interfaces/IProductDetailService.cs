using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IProductDetailService
    {
        Task CreateAsync(ProductDetailRequest dto);
        Task UpdateAsync(ProductDetailRequest dto);
        Task DeleteAsync(int id);
        Task<IEnumerable<ProductDetailResponse>> GetByParentIdAsync(int parentId);
    }
}
