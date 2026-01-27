using System.ComponentModel.DataAnnotations;

namespace TetGift.BLL.Dtos
{
    public class ProductCategoryDto
    {
        public int Categoryid { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        public string Categoryname { get; set; } = null!;
    }
}
