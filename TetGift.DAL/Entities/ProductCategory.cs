namespace TetGift.DAL.Entities;

/// <summary>
/// Categorizes products into groups like "Bánh ngọt", "Kẹo mứt", "Nước uống"
/// Foundation for the system to understand which products belong to which category
/// Used by ConfigDetail to define basket composition rules
/// Example: ConfigDetail says "Need 3 items from 'Bánh ngọt' category"
/// </summary>
public partial class ProductCategory
{
    public int Categoryid { get; set; }

    /// <summary>
    /// Name of the category (e.g., "Bánh ngọt", "Kẹo mứt", "Nước uống")
    /// </summary>
    public string Categoryname { get; set; } = null!;

    public bool? Isdeleted { get; set; }

    /// <summary>
    /// ConfigDetail rules that reference this category
    /// Defines how many items from this category each basket config requires
    /// </summary>
    public virtual ICollection<ConfigDetail> ConfigDetails { get; set; } = [];

    /// <summary>
    /// Individual products that belong to this category
    /// </summary>
    public virtual ICollection<Product> Products { get; set; } = [];
}
