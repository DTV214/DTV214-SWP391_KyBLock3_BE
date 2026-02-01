using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TetGift.DAL.Entities;

namespace TetGift.DAL.Interfaces
{
    public interface IProductRepository
    {
        // READ methods
        Task<IEnumerable<Product>> GetAllActiveProductsAsync();
        Task<Product?> GetProductByIdAsync(int productId);
        Task<Product?> GetProductWithDetailsAsync(int productId);
        Task<Product?> GetProductWithConfigAndDetailsAsync(int productId);
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<IEnumerable<Product>> GetProductsByStatusAsync(string status);
        Task<IEnumerable<Product>> GetComboProductsAsync(); // Products có Configid != null
        Task<IEnumerable<Product>> GetSingleProductsAsync(); // Products có Configid == null
        Task<Product?> GetProductBySkuAsync(string sku);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(int productId);
    }
}
