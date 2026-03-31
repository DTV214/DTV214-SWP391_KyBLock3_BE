using System;
using System.ComponentModel.DataAnnotations;

namespace TetGift.BLL.Dtos
{
    // DTO cho khách hàng gửi Form
    public class CreateContactRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập tên")]
        public string CustomerName { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress]
        public string Email { get; set; } = null!;

        public string? Note { get; set; }
    }

    // DTO cho Admin xem và sửa
    public class ContactAdminDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Note { get; set; }
        public bool IsContacted { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // DTO để Admin cập nhật (Cho phép sửa mọi trường như bạn yêu cầu)
    public class UpdateContactRequest
    {
        public string? CustomerName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Note { get; set; }
        public bool IsContacted { get; set; }
    }
}