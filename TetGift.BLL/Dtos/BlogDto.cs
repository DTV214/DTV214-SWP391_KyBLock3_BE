using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
// BỎ dòng using Microsoft.AspNetCore.Http; đi vì không cần IFormFile nữa

namespace TetGift.BLL.Dtos
{
    public class BlogDto
    {
        public int BlogId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? AuthorName { get; set; }
        public DateTime? CreationDate { get; set; }

        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
    }

    public class CreateBlogRequest
    {
        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Nội dung không được để trống")]
        public string Content { get; set; } = null!;

        // ĐỔI SANG KIỂU STRING ĐỂ NHẬN URL TỪ CLOUD
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
    }

    public class UpdateBlogRequest
    {
        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Nội dung không được để trống")]
        public string Content { get; set; } = null!;

        // ĐỔI SANG KIỂU STRING ĐỂ NHẬN URL TỪ CLOUD
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
    }
}