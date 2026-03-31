using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TetGift.DAL.Entities
{
    [Table("request_contact")] // Đặt tên bảng dưới DB là request_contact
    public partial class RequestContact
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("customer_name")]
        [StringLength(255)]
        public string CustomerName { get; set; } = null!;

        [Required]
        [Column("phone")]
        [StringLength(20)]
        public string Phone { get; set; } = null!;

        [Required]
        [EmailAddress]
        [Column("email")]
        [StringLength(255)]
        public string Email { get; set; } = null!;

        [Column("note")]
        public string? Note { get; set; }

        [Column("is_contacted")]
        public bool IsContacted { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}