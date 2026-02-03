namespace TetGift.DAL.Entities;

/// <summary>
/// Defines a template for gift baskets with overall constraints
/// Example: "Giỏ Tết Sang Trọng" with name, image, suitable suggestions, and max weight
/// The actual rules for which categories and how many items are defined in ConfigDetail
/// Products that reference this config must follow all ConfigDetail rules
/// </summary>
public partial class ProductConfig
{
    public int Configid { get; set; }

    /// <summary>
    /// Name of the basket template (e.g., "Giỏ Sang Trọng", "Giỏ Tết Vàng")
    /// </summary>
    public string? Configname { get; set; }

    public bool? Isdeleted { get; set; }

    /// <summary>
    /// Suggestions for who this basket is suitable for
    /// </summary>
    public string? Suitablesuggestion { get; set; }

    /// <summary>
    /// Maximum total weight/unit allowed for baskets using this config
    /// Validated when adding ProductDetail to ensure total doesn't exceed this limit
    /// </summary>
    public decimal? Totalunit { get; set; }

    public string? Imageurl { get; set; }

    /// <summary>
    /// Collection of rules defining required categories and quantities
    /// Each ConfigDetail specifies how many items from each category
    /// </summary>
    public virtual ICollection<ConfigDetail> ConfigDetails { get; set; } = [];

    /// <summary>
    /// Basket products that use this configuration template
    /// </summary>
    public virtual ICollection<Product> Products { get; set; } = [];
}
