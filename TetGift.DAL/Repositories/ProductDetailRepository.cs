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
    public class ProductDetailRepository : IProductDetailRepository
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<ProductDetail> _repository;

        public ProductDetailRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _repository = _unitOfWork.GetRepository<ProductDetail>();
        }

        public async Task<IEnumerable<ProductDetail>> GetDetailsByParentIdAsync(int parentId)
        {
            return await _repository.GetAllAsync(
                predicate: pd => pd.Productparentid == parentId,
                include: query => query
                    .Include(pd => pd.Product)
                        .ThenInclude(p => p.Category)
            );
        }

        public async Task<ProductDetail?> GetDetailByIdAsync(int detailId)
        {
            return await _repository.GetByIdAsync(detailId);
        }

        public async Task<ProductDetail?> GetDetailWithProductInfoAsync(int detailId)
        {
            return await _repository.FindAsync(
                predicate: pd => pd.Productdetailid == detailId,
                include: query => query
                    .Include(pd => pd.Product)
                    .Include(pd => pd.Productparent)
            );
        }

        public async Task<IEnumerable<ProductDetail>> GetDetailsByProductIdAsync(int productId)
        {
            return await _repository.GetAllAsync(
                predicate: pd => pd.Productid == productId
            );
        }

        public async Task UpdateDetailAsync(ProductDetail detail)
        {
            _repository.Update(detail);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteDetailAsync(int detailId)
        {
            var detail = await _repository.GetByIdAsync(detailId);
            if (detail == null)
                throw new Exception("Chi tiết sản phẩm không tồn tại.");

            _repository.Delete(detail);
            await _unitOfWork.SaveAsync();
        }
    }
}
