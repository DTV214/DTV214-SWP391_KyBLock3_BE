using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TetGift.BLL.Dtos
{
    public class UserProfileDto
    {
        public int AccountId { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }
        // Có thể thêm WalletBalance nếu cần sau này
        public decimal WalletBalance { get; set; }
    }

    public class UpdateProfileRequest
    {
        // Chỉ cho phép sửa các thông tin cá nhân cơ bản
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }
}
