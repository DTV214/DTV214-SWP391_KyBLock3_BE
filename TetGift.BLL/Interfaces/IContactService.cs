using System.Collections.Generic;
using System.Threading.Tasks;
using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IContactService
    {
        // Cho khách hàng
        Task CreateRequestAsync(CreateContactRequest req);

        // Cho Admin/Staff
        Task<IEnumerable<ContactAdminDto>> GetAllRequestsAsync();
        Task UpdateRequestAsync(int id, UpdateContactRequest req);
        Task DeleteRequestAsync(int id);
    }
}