using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TetGift.DAL.Context;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.DAL.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Product> _repository;
        private readonly DatabaseContext _context;

        public ProductRepository(IUnitOfWork unitOfWork, DatabaseContext context)
        {
            _unitOfWork = unitOfWork;
            _repository = _unitOfWork.GetRepository<Product>();
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllActiveProductsAsync()
        {
            return await _repository.GetAllAsync(
                predicate: p => p.Status == "Active",
                include: query => query.Include(p => p.Category)
            );
        }

        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await _repository.GetByIdAsync(productId);
        }

        public async Task<Product?> GetProductWithDetailsAsync(int productId)
        {
            return await _repository.FindAsync(
                predicate: p => p.Productid == productId,
                include: query => query
                    .Include(p => p.ProductDetailProductparents)
                        .ThenInclude(pd => pd.Product)
                            .ThenInclude(p => p.Category)
            );
        }

        public async Task<Product?> GetProductWithConfigAndDetailsAsync(int productId)
        {
            return await _repository.FindAsync(
                predicate: p => p.Productid == productId,
                include: query => query
                    .Include(p => p.Config)
                        .ThenInclude(c => c.ConfigDetails)
                            .ThenInclude(cd => cd.Category)
                    .Include(p => p.ProductDetailProductparents)
                        .ThenInclude(pd => pd.Product)
                            .ThenInclude(p => p.Category)
            );
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _repository.GetAllAsync(
                predicate: p => p.Categoryid == categoryId
            );
        }

        public async Task<IEnumerable<Product>> GetProductsByStatusAsync(string status)
        {
            return await _repository.GetAllAsync(
                predicate: p => p.Status == status
            );
        }

        public async Task<IEnumerable<Product>> GetComboProductsAsync()
        {
            return await _repository.GetAllAsync(
                predicate: p => p.Configid != null,
                include: query => query.Include(p => p.Config)
            );
        }

        public async Task<IEnumerable<Product>> GetSingleProductsAsync()
        {
            return await _repository.GetAllAsync(
                predicate: p => p.Configid == null,
                include: query => query.Include(p => p.Category)
            );
        }

        public async Task<Product?> GetProductBySkuAsync(string sku)
        {
            return (Product?)await _repository.FindAsync(
                predicate: p => p.Sku == sku
            );
        }

        public async Task UpdateProductAsync(Product product)
        {
            _repository.Update(product);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteProductAsync(int productId)
        {
            var product = await _repository.GetByIdAsync(productId);
            if (product == null)
                throw new Exception("Sản phẩm không tồn tại.");

            _repository.Delete(product);
            await _unitOfWork.SaveAsync();
        }

        public async Task<List<Product>> SearchAsync(string? keyword, string? category, decimal? maxPrice)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.Status == "Active");

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(p => EF.Functions.ILike(p.Productname ?? "", $"%{keyword}%"));

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category != null &&
                                         EF.Functions.ILike(p.Category.Categoryname, $"%{category}%"));

            if (maxPrice.HasValue && maxPrice.Value > 0)
                query = query.Where(p => p.Price <= maxPrice.Value);

            return await query
                .OrderBy(p => p.Price)
                .Take(10)
                .ToListAsync();
        }

        public async Task<List<Product>> GetByIdsAsync(List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return new List<Product>();

            return await _context.Products
                .Include(p => p.Category)
                .Where(p => ids.Contains(p.Productid) && p.Status == "Active")
                .ToListAsync();
        }
    }
}
