using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TetGift.DAL.Entities;

namespace TetGift.DAL.Interfaces
{
    public interface IProductConfigRepository
    {
        // READ methods
        Task<IEnumerable<ProductConfig>> GetAllActiveConfigsAsync();
        Task<ProductConfig?> GetConfigByIdAsync(int configId);
        Task<ProductConfig?> GetConfigWithDetailsAsync(int configId);
        Task<IEnumerable<ProductConfig>> GetConfigsBySuggestionAsync(string suggestion);
        Task UpdateConfigAsync(ProductConfig config);
        Task DeleteConfigAsync(int configId);
    }
}
