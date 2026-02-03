using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IBlogService
    {
        Task<IEnumerable<BlogDto>> GetAllAsync();
        Task<BlogDto> GetByIdAsync(int id);

        // Hàm Create cần thêm accountId (người tạo)
        Task<BlogDto> CreateAsync(int accountId, CreateBlogRequest req);

        Task UpdateAsync(int id, UpdateBlogRequest req);
        Task DeleteAsync(int id);

    }
}
