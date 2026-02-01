using Microsoft.EntityFrameworkCore;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services;

public class ProductService(IUnitOfWork uow) : IProductService
{
    private readonly IUnitOfWork _uow = uow;

    public async Task CreateNormalAsync(ProductDto dto)
    {
        if (dto.Categoryid == null || !dto.Categoryid.HasValue
            || dto.Accountid == null || !dto.Accountid.HasValue
            || string.IsNullOrWhiteSpace(dto.Sku)
            || string.IsNullOrWhiteSpace(dto.Productname)
            || dto.Price == null || !dto.Price.HasValue || dto.Price.Value <= 0
            || dto.Unit == null || !dto.Unit.HasValue || dto.Unit.Value <= 0)
        {
            throw new Exception("Thiếu thông tin bắt buộc cho sản phẩm thường (Category, Account, SKU, Name, Price, Unit).");
        }

        await ValidateForeignKeys(dto.Accountid, null, dto.Categoryid);

        var entity = MapToEntity(dto);
        await _uow.GetRepository<Product>().AddAsync(entity);
        await _uow.SaveAsync();
    }

    public async Task CreateCustomAsync(ProductDto dto)
    {
        if (dto.Accountid == null || !dto.Accountid.HasValue
            || string.IsNullOrWhiteSpace(dto.Productname))
        {
            throw new Exception("Thiếu thông tin bắt buộc cho sản phẩm tùy chỉnh (ConfigId, AccountId, ProductName).");
        }

        await ValidateForeignKeys(dto.Accountid, dto.Configid, null);
        dto.Unit = 0;
        dto.Price = 0;

        var entity = MapToEntity(dto);
        await _uow.GetRepository<Product>().AddAsync(entity);
        await _uow.SaveAsync();
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync()
    {
        var products = await _uow.GetRepository<Product>().GetAllAsync(
            p => !p.Account.Role.Equals("CUSTOMER") && !p.Status.Equals("DELETED"),
            include: p => p.Include(p => p.ProductDetailProductparents).ThenInclude(pd => pd.Product)
            );
        return products.Select(p =>
        {
            p.CalculateUnit();
            p.CalculateTotalPrice();

            return new ProductDto()
            {
                Productid = p.Productid,
                Categoryid = p.Categoryid,
                Configid = p.Configid,
                Accountid = p.Accountid,
                Sku = p.Sku,
                Productname = p.Productname,
                Description = p.Description,
                Price = p.Price,
                Status = p.Status,
                Unit = p.Unit,
                ImageUrl = p.ImageUrl
            };
        });
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var product = await _uow.GetRepository<Product>().FindAsync(
            p => p.Productid == id && !p.Status.Equals("DELETED"),
            include: p => p.Include(p => p.ProductDetailProductparents).ThenInclude(pd => pd.Product)
            );
        if (product == null) return null;
        product.CalculateUnit();
        product.CalculateTotalPrice();

        return new ProductDto
        {
            Productid = product.Productid,
            Categoryid = product.Categoryid,
            Configid = product.Configid,
            Accountid = product.Accountid,
            Sku = product.Sku,
            Productname = product.Productname,
            Description = product.Description,
            Price = product.Price,
            Status = product.Status,
            Unit = product.Unit,
            ImageUrl = product.ImageUrl
        };
    }

    public async Task<IEnumerable<ProductDto>> GetByAccountIdAsync(int accountId)
    {
        var products = await _uow.GetRepository<Product>()
            .GetAllAsync(p => p.Accountid == accountId && !p.Status.Equals("DELETED"),
            include: p => p.Include(p => p.ProductDetailProductparents).ThenInclude(pd => pd.Product)
            );

        return products.Select(p =>
        {
            p.CalculateUnit();
            p.CalculateTotalPrice();
            return new ProductDto
            {
                Productid = p.Productid,
                Categoryid = p.Categoryid,
                Configid = p.Configid,
                Accountid = p.Accountid,
                Sku = p.Sku,
                Productname = p.Productname,
                Description = p.Description,
                Price = p.Price,
                Status = p.Status,
                Unit = p.Unit,
                ImageUrl = p.ImageUrl
            };
        });
    }

    private Product MapToEntity(ProductDto dto)
    {
        return new Product
        {
            Categoryid = dto.Categoryid,
            Configid = dto.Configid,
            Accountid = dto.Accountid,
            Sku = dto.Sku,
            Productname = dto.Productname,
            Description = dto.Description,
            Price = dto.Price,
            Status = dto.Status ?? "ACTIVE",
            Unit = dto.Unit,
            ImageUrl = dto.ImageUrl
        };
    }

    public async Task DeleteAsync(int id)
    {
        var repo = _uow.GetRepository<Product>();
        var entity = await repo.GetByIdAsync(id);
        if (entity != null)
        {
            entity.Status = "DELETED";
            repo.Update(entity);
            await _uow.SaveAsync();
        }
    }

    private async Task ValidateForeignKeys(int? accountId, int? configId, int? categoryId)
    {
        if (accountId.HasValue)
        {
            var account = await _uow.GetRepository<Account>().FindAsync(
                a => a.Accountid == accountId.Value && a.Status.Equals("ACTIVE")
                );
            if (!account.Any()) throw new Exception($"AccountId {accountId} không tồn tại.");
        }

        if (configId.HasValue)
        {
            var config = await _uow.GetRepository<ProductConfig>().FindAsync(
                c => c.Configid == configId.Value && c.Isdeleted == false
                );
            if (!config.Any()) throw new Exception($"ConfigId {configId} không tồn tại.");
        }

        if (categoryId.HasValue)
        {
            var category = await _uow.GetRepository<ProductCategory>().FindAsync(
                c => c.Categoryid == categoryId.Value && c.Isdeleted == false
                );
            if (!category.Any()) throw new Exception($"CategoryId {categoryId} không tồn tại.");
        }
    }

    public async Task<UpdateProductDto> UpdateNormalAsync(ProductDto dto)
    {
        var repo = _uow.GetRepository<Product>();
        var result = await repo.FindAsync(
            e => e.Productid == dto.Productid && !e.Status.Equals("DELETED")
            );
        if (!result.Any()) throw new Exception("Không tìm thấy sản phẩm.");
        var entity = result.First();

        #region Validation Cate, Blank String, Number Property

        // 1. Validate CategoryId
        if (dto.Categoryid.HasValue)
        {
            await ValidateForeignKeys(null, null, dto.Categoryid);
            entity.Categoryid = dto.Categoryid;
        }

        // 2. Validate blank string 
        if (dto.Sku != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Sku)) throw new Exception("SKU không được để trống.");
            entity.Sku = dto.Sku;
        }
        if (dto.Productname != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Productname)) throw new Exception("Tên không được để trống.");
            entity.Productname = dto.Productname;
        }
        if (dto.Status != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Status)
                || (!dto.Status.Equals("ACTIVE") && !dto.Status.Equals("INACTIVE")
                && !dto.Status.Equals("OUT_OF_STOCK")
                )) throw new Exception("Trạng thái không được để trống hoặc sai syntax.");
            entity.Status = dto.Status;
        }

        // 3. Validate số dương
        if (dto.Price.HasValue)
        {
            if (dto.Price <= 0) throw new Exception("Giá phải lớn hơn 0.");
            entity.Price = dto.Price;
        }
        if (dto.Unit.HasValue)
        {
            if (dto.Unit <= 0) throw new Exception("Đơn vị phải lớn hơn 0.");
            entity.Unit = dto.Unit;
        }

        #endregion

        repo.Update(entity);
        await _uow.SaveAsync();
        return new UpdateProductDto();
    }

    public async Task<UpdateProductDto> UpdateCustomAsync(ProductDto dto)
    {
        var repo = _uow.GetRepository<Product>();
        var result = await repo.FindAsync(
            e => e.Productid == dto.Productid && !e.Status.Equals("DELETED")
            );
        if (!result.Any()) throw new Exception("Không tìm thấy sản phẩm.");
        var entity = result.First();

        var response = new UpdateProductDto();

        #region Validation Cate, Blank String, Number Property

        // 1. Validate ConfigId và Logic Unit
        if (dto.Configid.HasValue)
        {
            var configResult = await _uow.GetRepository<ProductConfig>().FindAsync(
                            c => c.Configid == dto.Configid.Value && c.Isdeleted == false
                            );
            if (!configResult.Any()) throw new Exception($"ConfigId {dto.Configid} không tồn tại.");
            var config = configResult.First();

            entity.Configid = dto.Configid;

            // So sánh Unit của Product với Totalunit của Config
            decimal currentUnit = dto.Unit ?? entity.Unit ?? 0;
            if (config.Totalunit.HasValue && currentUnit > config.Totalunit.Value)
            {
                response.Warning = $"Cảnh báo: Đơn vị sản phẩm ({currentUnit}) vượt quá hạn mức cấu hình ({config.Totalunit.Value}).";
            }
        }

        // 2. Validate string không trống
        if (dto.Productname != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Productname)) throw new Exception("Tên không được để trống.");
            entity.Productname = dto.Productname;
        }
        if (dto.Status != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Status)
                || (!dto.Status.Equals("ACTIVE") && !dto.Status.Equals("INACTIVE")
                && !dto.Status.Equals("OUT_OF_STOCK")
                )) throw new Exception("Trạng thái không được để trống hoặc sai syntax.");
            entity.Status = dto.Status;
        }

        #endregion

        repo.Update(entity);
        await _uow.SaveAsync();
        return response;
    }
}