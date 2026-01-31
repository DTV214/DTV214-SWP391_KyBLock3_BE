namespace TetGift.DAL.Entities
{
    public partial class QuotationCategoryRequest
    {
        public int Quotationcategoryrequestid { get; set; }
        public int Quotationid { get; set; }
        public int Categoryid { get; set; }

        public int? Quantity { get; set; }         // optional
        public string? Note { get; set; }
        public DateTime Createdat { get; set; }

        public virtual Quotation Quotation { get; set; } = null!;
        public virtual ProductCategory? Category { get; set; }
    }
}
