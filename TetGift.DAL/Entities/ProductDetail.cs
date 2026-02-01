namespace TetGift.DAL.Entities;

/// <summary>
/// Links a parent Product (basket) to a child Product (individual item) with quantity
/// Example: Basket "Giỏ Tết ID=100" contains:
///   - ProductDetail(Productparentid=100, Productid=10, Quantity=2) -> 2x "Bánh Kem Dâu"
///   - ProductDetail(Productparentid=100, Productid=15, Quantity=1) -> 1x "Kẹo Dừa"
/// When adding ProductDetail, the system validates:
///   - The child product's category is allowed by the parent's ConfigDetail
///   - The quantity doesn't exceed the ConfigDetail limit for that category
///   - The total weight doesn't exceed ProductConfig.Totalunit
/// </summary>
public partial class ProductDetail
{
    public int Productdetailid { get; set; }

    /// <summary>
    /// The basket product (parent) that contains this item
    /// </summary>
    public int? Productparentid { get; set; }

    /// <summary>
    /// The individual product (child) being added to the basket
    /// </summary>
    public int? Productid { get; set; }

    /// <summary>
    /// Quantity of this product in the basket
    /// Validated against ConfigDetail.Quantity for the product's category
    /// </summary>
    public int? Quantity { get; set; }

    public virtual Product? Product { get; set; }

    public virtual Product? Productparent { get; set; }
}
