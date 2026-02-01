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
    public class ProductConfigRepository : IProductConfigRepository
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<ProductConfig> _repository;

        public ProductConfigRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _repository = _unitOfWork.GetRepository<ProductConfig>();
        }

        public async Task<IEnumerable<ProductConfig>> GetAllActiveConfigsAsync()
        {
            return await _repository.GetAllAsync(
                predicate: c => c.Isdeleted == false || c.Isdeleted == null
            );
        }

        public async Task<ProductConfig?> GetConfigByIdAsync(int configId)
        {
            return (ProductConfig?)await _repository.FindAsync(
                predicate: c => c.Configid == configId && (c.Isdeleted == false || c.Isdeleted == null)
            );
        }

        public async Task<ProductConfig?> GetConfigWithDetailsAsync(int configId)
        {
            return await _repository.FindAsync(
                predicate: c => c.Configid == configId && (c.Isdeleted == false || c.Isdeleted == null),
                include: query => query
                    .Include(c => c.ConfigDetails)
                        .ThenInclude(cd => cd.Category)
            );
        }

        public async Task<IEnumerable<ProductConfig>> GetConfigsBySuggestionAsync(string suggestion)
        {
            return await _repository.GetAllAsync(
                predicate: c => (c.Isdeleted == false || c.Isdeleted == null)
                    && c.Suitablesuggestion != null
                    && c.Suitablesuggestion.Contains(suggestion)
            );
        }

        public async Task UpdateConfigAsync(ProductConfig config)
        {
            _repository.Update(config);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteConfigAsync(int configId)
        {
            var config = await _repository.GetByIdAsync(configId);
            if (config == null)
                throw new Exception("Cấu hình không tồn tại.");

            config.Isdeleted = true;
            _repository.Update(config);
            await _unitOfWork.SaveAsync();
        }
    }
}
