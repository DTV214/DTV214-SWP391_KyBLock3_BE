using Microsoft.EntityFrameworkCore;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services;

public class ProductDetailService(IUnitOfWork uow) : IProductDetailService
{
    private readonly IUnitOfWork _uow = uow;

    private async Task<Product> GetById(int id)
    {
        var product = await _uow.GetRepository<Product>().FindAsync(
            p => p.Productid == id && !p.Status.Equals("DELETED"),
            p => p
            .Include(pr => pr.Config).ThenInclude(pr => pr.ConfigDetails)
            .Include(pr => pr.ProductDetailProductparents)
            );

        return product ?? throw new Exception("Không tìm thấy product id: " + id);
    }

    public async Task CreateAsync(ProductDetailRequest dto)
    {
        // Validation
        if (dto.Quantity == null || dto.Quantity <= 0)
            throw new Exception("Quantity phải là số nguyên dương lớn hơn 0.");

        if (!dto.Productid.HasValue || !dto.Productparentid.HasValue)
            throw new Exception("ProductId và ProductParentId là bắt buộc.");

        var product = await GetById(dto.Productid.Value);
        var parent = await GetById(dto.Productparentid.Value);
        if (!parent.Configid.HasValue) throw new Exception("Sản phẩm cha không hợp lệ: " + parent.Productid);

        // Dulicate validation
        parent.EnsureProductNotDuplicate(product.Productid);

        // Config logic validation
        parent.ValidateDetailAgainstConfig(product.Categoryid, dto.Quantity);

        // Create Entity
        var entity = new ProductDetail
        {
            Productparentid = dto.Productparentid,
            Productid = dto.Productid,
            Quantity = dto.Quantity
        };

        await _uow.GetRepository<ProductDetail>().AddAsync(entity);

        // Update Parent
        parent.Unit += (product.Unit * entity.Quantity);

        if (parent.Config != null && parent.Config.Totalunit < parent.Unit)
            throw new Exception("Số lượng vượt quá cấu hình");

        parent.Price += (product.Price * entity.Quantity);
        _uow.GetRepository<Product>().Update(parent);

        await _uow.SaveAsync();
    }

    public async Task UpdateAsync(ProductDetailRequest dto)
    {
        var repo = _uow.GetRepository<ProductDetail>();
        var entity = await repo.FindAsync(
            pd => pd.Productdetailid == dto.Productdetailid,
            query => query
            .Include(pd => pd.Product)
            .Include(pd => pd.Productparent).ThenInclude(pp => pp.Config).ThenInclude(c => c.ConfigDetails)
            ) ?? throw new Exception("Không tìm thấy chi tiết sản phẩm.");
        var parent = entity.Productparent;
        parent.Unit -= (entity?.Product?.Unit * entity?.Quantity);
        parent.Price -= (entity?.Product?.Price * entity?.Quantity);

        // Config detail logic validation
        parent.ValidateUpdateDetailAgainstConfig(entity.Product.Categoryid, dto.Quantity, dto.Productdetailid.Value);


        if (dto.Productid.HasValue)
        {
            await GetById(dto.Productid.Value);
            entity.Productid = dto.Productid;
        }

        if (dto.Quantity.HasValue)
        {
            if (dto.Quantity <= 0) throw new Exception("Quantity phải lớn hơn 0.");
            entity.Quantity = dto.Quantity;
        }

        repo.Update(entity);

        // Update Parent
        parent.Unit += (entity?.Product?.Unit * entity?.Quantity);

        // Validate Unit Total From Config
        if (parent.Config != null && parent.Config.Totalunit < parent.Unit)
            throw new Exception("Số lượng vượt quá cấu hình");

        parent.Price += (entity?.Product?.Price * entity?.Quantity);
        _uow.GetRepository<Product>().Update(parent);

        await _uow.SaveAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var repo = _uow.GetRepository<ProductDetail>();
        var entity = await repo.FindAsync(pd => pd.Productdetailid == id, pd => pd.Include(p => p.Product).Include(p => p.Productparent));

        if (entity != null)
        {
            // Update Parent
            var parent = entity.Productparent;
            parent.Unit -= (entity.Product.Unit * entity.Quantity);
            parent.Price -= (entity?.Product?.Price * entity?.Quantity);

            repo.Delete(entity);
            _uow.GetRepository<Product>().Update(parent);

            await _uow.SaveAsync();
        }
    }

    public async Task<IEnumerable<ProductDetailResponse>> GetByParentIdAsync(int parentId)
    {
        var details = await _uow.GetRepository<ProductDetail>()
                                .GetAllAsync(d => d.Productparentid == parentId, query => query.Include(d => d.Product));

        var responseList = new List<ProductDetailResponse>();

        foreach (var d in details)
        {
            var product = d.Product;

            responseList.Add(new ProductDetailResponse
            {
                Productdetailid = d.Productdetailid,
                Productparentid = d.Productparentid,
                Productid = d.Productid,
                Quantity = d.Quantity,
                Productname = product?.Productname,
                Unit = product?.Unit,
                Price = product?.Price,
                //Imageurl = product.Url
            });
        }

        return responseList;
    }
}