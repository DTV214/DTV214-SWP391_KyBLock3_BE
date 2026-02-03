using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TetGift.DAL.Entities;

namespace TetGift.DAL.Interfaces
{
    public interface IProductCategoryRepository
    {
        // READ methods
        Task<IEnumerable<ProductCategory>> GetAllActiveCategoriesAsync();
        Task<ProductCategory?> GetCategoryByIdAsync(int categoryId);
        Task<ProductCategory?> GetCategoryWithProductsAsync(int categoryId);        
        Task UpdateCategoryAsync(ProductCategory category);       
        Task DeleteCategoryAsync(int categoryId);
    }
}
