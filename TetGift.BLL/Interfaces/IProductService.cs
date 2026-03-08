using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllAsync();
    Task<ProductDto?> GetByIdAsync(int id);
    Task<IEnumerable<ProductDto>> GetByAccountIdAsync(int accountId); // cho admin lấy giỏ mẫu
    Task<IEnumerable<CustomerBasketDto>> GetCustomerBasketsByAccountIdAsync(int accountId); //  cho customer lấy giỏ tự tạo
    Task CreateNormalAsync(CreateSingleProductRequest dto); // admin tạo sản phẩm thường
    Task<int> CreateCustomAsync(CreateComboProductRequest dto); // tạo nguyên 1 giỏ quà, trả về productid
    Task<UpdateProductDto> UpdateNormalAsync(ProductDto dto);
    Task<UpdateProductDto> UpdateCustomAsync(int productId, UpdateComboProductRequest dto, int? requestingAccountId);
    Task DeleteAsync(int id);
    Task<ProductValidationDto> GetProductValidationStatus(int productId);
    Task<ProductDto> CloneBasketAsync(int templateId, int customerId, string? customName);
    Task<IEnumerable<ProductDto>> GetTemplatesAsync();
    Task<IEnumerable<ProductDto>> GetAdminBasketsAsync(); // Lấy giỏ quà do admin/staff tạo
    Task<IEnumerable<ProductDto>> GetShopBasketsAsync(); // Lấy giỏ quà ACTIVE của admin/staff cho trang shop
    Task<ProductDto?> GetCustomProductByIdAsync(int productId); // Lấy chi tiết sản phẩm custom/basket để edit
    Task SetAsTemplateAsync(int productId);
    Task RemoveTemplateAsync(int productId);
    Task HardDeleteTemplateAsync(int id);
    Task<PagedResponse<ProductDto>> GetWithQueryAsync(ProductQueryParameters productQuery);
}