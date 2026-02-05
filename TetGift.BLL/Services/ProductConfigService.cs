using Microsoft.EntityFrameworkCore;
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
            var data = await _uow.GetRepository<ProductConfig>().GetAllAsync(
                pc => pc.Isdeleted == false,
                include: q => q.Include(pc => pc.Products)
                    .ThenInclude(p => p.ProductDetailProductparents)
                        .ThenInclude(pd => pd.Product)
                    .Include(pc => pc.ConfigDetails)
                        .ThenInclude(cd => cd.Category)
            );

            return data.Select(x => new ProductConfigDto
            {
                Configid = x.Configid,
                Configname = x.Configname,
                Suitablesuggestion = x.Suitablesuggestion,
                Totalunit = x.Totalunit,
                Imageurl = x.Imageurl,
                ConfigDetails = x.ConfigDetails?.Select(cd => new ConfigDetailDto
                {
                    Configdetailid = cd.Configdetailid,
                    Configid = cd.Configid ?? 0,
                    Categoryid = cd.Categoryid ?? 0,
                    CategoryName = cd.Category?.Categoryname ?? "",
                    Quantity = cd.Quantity ?? 0
                }).ToList() ?? new List<ConfigDetailDto>(),
                Products = x.Products
                    // .Where(p => p.Status == "TEMPLATE")
                    .Select(p =>
                    {
                        p.CalculateUnit();
                        p.CalculateTotalPrice();
                        return new ProductDto
                        {
                            Productid = p.Productid,
                            Configid = p.Configid,
                            Accountid = p.Accountid,
                            Productname = p.Productname,
                            Description = p.Description,
                            ImageUrl = p.ImageUrl,
                            Price = p.Price,
                            Unit = p.Unit,
                            Status = p.Status,
                            IsCustom = true,
                            ProductDetails = p.ProductDetailProductparents?.Select(pd => new ProductDetailResponse
                            {
                                Productdetailid = pd.Productdetailid,
                                Productid = pd.Productid,
                                Productparentid = pd.Productparentid,
                                Quantity = pd.Quantity,
                                ChildProduct = pd.Product != null ? new ProductDto
                                {
                                    Productid = pd.Product.Productid,
                                    Categoryid = pd.Product.Categoryid,
                                    Productname = pd.Product.Productname,
                                    Description = pd.Product.Description,
                                    ImageUrl = pd.Product.ImageUrl,
                                    Price = pd.Product.Price,
                                    Unit = pd.Product.Unit,
                                    Status = pd.Product.Status
                                } : null
                            }).ToList()
                        };
                    }).ToList()
            });
        }

        public async Task<ProductConfigDto?> GetByIdAsync(int id)
        {
            var configs = await _uow.GetRepository<ProductConfig>().FindAsync(
                pc => pc.Configid == id && pc.Isdeleted == false,
                include: q => q.Include(pc => pc.Products)
                    .ThenInclude(p => p.ProductDetailProductparents)
                        .ThenInclude(pd => pd.Product)
                    .Include(pc => pc.ConfigDetails)
                        .ThenInclude(cd => cd.Category)
            );

            var config = configs as IEnumerable<ProductConfig>;
            if (config == null || !config.Any()) return null;

            var result = config.First();

            return new ProductConfigDto
            {
                Configid = result.Configid,
                Configname = result.Configname,
                Suitablesuggestion = result.Suitablesuggestion,
                Totalunit = result.Totalunit,
                Imageurl = result.Imageurl,
                ConfigDetails = result.ConfigDetails?.Select(cd => new ConfigDetailDto
                {
                    Configdetailid = cd.Configdetailid,
                    Configid = cd.Configid ?? 0,
                    Categoryid = cd.Categoryid ?? 0,
                    CategoryName = cd.Category?.Categoryname ?? "",
                    Quantity = cd.Quantity ?? 0
                }).ToList() ?? new List<ConfigDetailDto>(),
                Products = result.Products
                    //.Where(p => p.Status == "TEMPLATE")
                    .Select(p =>
                    {
                        p.CalculateUnit();
                        p.CalculateTotalPrice();
                        return new ProductDto
                        {
                            Productid = p.Productid,
                            Configid = p.Configid,
                            Accountid = p.Accountid,
                            Productname = p.Productname,
                            Description = p.Description,
                            ImageUrl = p.ImageUrl,
                            Price = p.Price,
                            Unit = p.Unit,
                            Status = p.Status,
                            IsCustom = true,
                            ProductDetails = p.ProductDetailProductparents?.Select(pd => new ProductDetailResponse
                            {
                                Productdetailid = pd.Productdetailid,
                                Productid = pd.Productid,
                                Productparentid = pd.Productparentid,
                                Quantity = pd.Quantity,
                                ChildProduct = pd.Product != null ? new ProductDto
                                {
                                    Productid = pd.Product.Productid,
                                    Categoryid = pd.Product.Categoryid,
                                    Productname = pd.Product.Productname,
                                    Description = pd.Product.Description,
                                    ImageUrl = pd.Product.ImageUrl,
                                    Price = pd.Product.Price,
                                    Unit = pd.Product.Unit,
                                    Status = pd.Product.Status
                                } : null
                            }).ToList()
                        };
                    }).ToList()
            };
        }

        public async Task<int> CreateAsync(CreateConfigRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Configname))
                throw new Exception("Tên cấu hình không được để trống.");

            // Create ProductConfig
            var config = new ProductConfig
            {
                Configname = request.Configname,
                Suitablesuggestion = request.Description,
                Totalunit = request.Totalunit,
                Imageurl = null
            };
            await _uow.GetRepository<ProductConfig>().AddAsync(config);
            await _uow.SaveAsync();

            // Create ConfigDetails for each category with quantity > 0
            var configDetailRepo = _uow.GetRepository<ConfigDetail>();
            foreach (var kvp in request.CategoryQuantities.Where(kv => kv.Value > 0))
            {
                var detail = new ConfigDetail
                {
                    Configid = config.Configid,
                    Categoryid = kvp.Key,
                    Quantity = kvp.Value
                };
                await configDetailRepo.AddAsync(detail);
            }
            await _uow.SaveAsync();

            return config.Configid;
        }

        public async Task UpdateAsync(int configId, UpdateConfigRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Configname))
                throw new Exception("Tên không được để trống.");

            var repo = _uow.GetRepository<ProductConfig>();
            var entity = await repo.GetByIdAsync(configId);
            if (entity == null || entity.Isdeleted == true)
                throw new Exception("Không tìm thấy cấu hình");

            // Update ProductConfig
            entity.Configname = request.Configname;
            entity.Suitablesuggestion = request.Description;
            entity.Totalunit = request.Totalunit;
            repo.Update(entity);

            // Delete existing ConfigDetails
            var configDetailRepo = _uow.GetRepository<ConfigDetail>();
            var existingDetails = await configDetailRepo.FindAsync(cd => cd.Configid == configId);
            if (existingDetails != null)
            {
                foreach (var detail in existingDetails)
                {
                    configDetailRepo.Delete(detail);
                }
            }

            // Create new ConfigDetails
            foreach (var kvp in request.CategoryQuantities.Where(kv => kv.Value > 0))
            {
                var detail = new ConfigDetail
                {
                    Configid = configId,
                    Categoryid = kvp.Key,
                    Quantity = kvp.Value
                };
                await configDetailRepo.AddAsync(detail);
            }

            await _uow.SaveAsync();
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
