namespace TetGift.DAL.Entities;

public partial class ProductDetail
{
    public int Productdetailid { get; set; }

    public int? Productparentid { get; set; }

    public int? Productid { get; set; }

    public int? Quantity { get; set; }

    public virtual Product? Product { get; set; }

    public virtual Product? Productparent { get; set; }
}
