using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllAsync();
    Task<ProductDto?> GetByIdAsync(int id);
    Task<IEnumerable<ProductDto>> GetByAccountIdAsync(int accountId);
    Task CreateNormalAsync(ProductDto dto);
    Task CreateCustomAsync(ProductDto dto);
    Task<UpdateProductDto> UpdateNormalAsync(ProductDto dto);
    Task<UpdateProductDto> UpdateCustomAsync(ProductDto dto, bool isCustomer);
    Task DeleteAsync(int id);
    Task<ProductValidationDto> GetProductValidationStatus(int productId);
    Task<ProductDto> CloneBasketAsync(int templateId, int customerId, string? customName);
    Task<IEnumerable<ProductDto>> GetTemplatesAsync();
    Task SetAsTemplateAsync(int productId);
    Task RemoveTemplateAsync(int productId);
}