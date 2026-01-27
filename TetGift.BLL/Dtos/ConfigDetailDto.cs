using System.ComponentModel.DataAnnotations;

namespace TetGift.BLL.Dtos;

public class ConfigDetailDto
{
    public int Configdetailid { get; set; }
    public int? Configid { get; set; }

    [Required(ErrorMessage = "Categoryid là bắt buộc")]
    public int? Categoryid { get; set; }

    [Required(ErrorMessage = "Số lượng là bắt buộc")]
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public int? Quantity { get; set; }
}