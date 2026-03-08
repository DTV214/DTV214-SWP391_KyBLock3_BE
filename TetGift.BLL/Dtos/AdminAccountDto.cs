using System.ComponentModel.DataAnnotations;

namespace TetGift.BLL.Dtos
{
    // 1. DTO Trả về danh sách/chi tiết (Ẩn Password và OTP)
    public class AccountAdminDto
    {
        public int AccountId { get; set; }
        public string Username { get; set; } = null!;
        public string? Email { get; set; }
        public string? Fullname { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }
    }

    // 2. DTO Tạo tài khoản mới (Admin cấp tài khoản cho Staff/Admin khác)
    public class CreateAccountAdminRequest
    {
        [Required(ErrorMessage = "Username là bắt buộc")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Password là bắt buộc")]
        public string Password { get; set; } = null!;

        public string? Email { get; set; }
        public string? Fullname { get; set; }
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Role là bắt buộc")]
        public string Role { get; set; } = "STAFF"; // Mặc định là STAFF
    }

    // 3. DTO Cập nhật (CHỈ CHO PHÉP SỬA STATUS)
    public class UpdateAccountStatusRequest
    {
        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        public string Status { get; set; } = null!;
    }
}