using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TetGift.DAL.Entities;

namespace TetGift.DAL.Interfaces
{
    public interface IProductDetailRepository
    {
        // READ methods
        Task<IEnumerable<ProductDetail>> GetDetailsByParentIdAsync(int parentId);
        Task<ProductDetail?> GetDetailByIdAsync(int detailId);
        Task<ProductDetail?> GetDetailWithProductInfoAsync(int detailId);
        Task<IEnumerable<ProductDetail>> GetDetailsByProductIdAsync(int productId);
        Task UpdateDetailAsync(ProductDetail detail);
        Task DeleteDetailAsync(int detailId);
    }
}
