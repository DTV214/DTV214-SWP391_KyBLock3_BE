namespace TetGift.DAL.Entities;

public partial class ProductCategory
{
    public int Categoryid { get; set; }

    public string Categoryname { get; set; } = null!;

    public bool? Isdeleted { get; set; }

    public virtual ICollection<ConfigDetail> ConfigDetails { get; set; } = [];

    public virtual ICollection<Product> Products { get; set; } = [];
}
