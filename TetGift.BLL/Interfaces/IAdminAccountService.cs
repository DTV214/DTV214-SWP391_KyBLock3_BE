using System.Collections.Generic;
using System.Threading.Tasks;
using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IAdminAccountService
    {
        Task<IEnumerable<AccountAdminDto>> GetAllAccountsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<AccountAdminDto> GetAccountByIdAsync(int id);
        Task<AccountAdminDto> CreateAccountAsync(CreateAccountAdminRequest req);

        // Hàm Update chỉ nhận vào request có chứa Status
        Task UpdateStatusAsync(int id, UpdateAccountStatusRequest req);

        // Hàm Delete (Soft Delete)
        Task DeleteAccountAsync(int id);
    }
}