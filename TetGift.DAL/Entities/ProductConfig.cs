namespace TetGift.DAL.Entities;

public partial class ProductConfig
{
    public int Configid { get; set; }

    public string? Configname { get; set; }

    public bool? Isdeleted { get; set; }

    public string? Suitablesuggestion { get; set; }

    public decimal? Totalunit { get; set; }

    public string? Imageurl { get; set; }

    public virtual ICollection<ConfigDetail> ConfigDetails { get; set; } = [];

    public virtual ICollection<Product> Products { get; set; } = [];
}
