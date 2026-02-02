using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IAccountService
    {
        // Lấy thông tin profile
        Task<UserProfileDto> GetProfileAsync(int accountId);

        // Cập nhật thông tin
        Task UpdateProfileAsync(int accountId, UpdateProfileRequest req);

        // Tự khóa tài khoản (Soft Delete)
        Task DeactivateAccountAsync(int accountId);
    }
}
