using System.ComponentModel.DataAnnotations;

namespace TetGift.BLL.Dtos
{
    // DTO chi tiết ConfigDetail
    public class ConfigDetailDto
    {
        public int? Configdetailid { get; set; }
        public int Configid { get; set; }
        public int Categoryid { get; set; }
        public string CategoryName { get; set; } = null!;
        public int Quantity { get; set; }
    }
}