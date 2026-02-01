using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.DAL.Repositories
{
    public  class ProductCategoryRepository
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<ProductCategory> _repository;

        public ProductCategoryRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _repository = _unitOfWork.GetRepository<ProductCategory>();
        }

        public async Task<IEnumerable<ProductCategory>> GetAllActiveCategoriesAsync()
        {
            return await _repository.GetAllAsync(
                predicate: c => c.Isdeleted == false || c.Isdeleted == null
            );
        }

        public async Task<ProductCategory?> GetCategoryByIdAsync(int categoryId)
        {
            return (ProductCategory?)await _repository.FindAsync(
                predicate: c => c.Categoryid == categoryId && (c.Isdeleted == false || c.Isdeleted == null)
            );
        }

        public async Task<ProductCategory?> GetCategoryWithProductsAsync(int categoryId)
        {
            return await _repository.FindAsync(
                predicate: c => c.Categoryid == categoryId && (c.Isdeleted == false || c.Isdeleted == null),
                include: query => query
                    .Include(c => c.Products)
                    .Include(c => c.ConfigDetails)
            );
        }

        public async Task UpdateCategoryAsync(ProductCategory category)
        {
            _repository.Update(category);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteCategoryAsync(int categoryId)
        {
            var category = await _repository.GetByIdAsync(categoryId);
            if (category == null)
                throw new Exception("Danh mục không tồn tại.");

            category.Isdeleted = true;
            _repository.Update(category);
            await _unitOfWork.SaveAsync();
        }
    }
}
