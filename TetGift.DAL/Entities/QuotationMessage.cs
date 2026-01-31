namespace TetGift.DAL.Entities
{
    public partial class QuotationMessage
    {
        public int Quotationmessageid { get; set; }
        public int Quotationid { get; set; }

        public string? Fromrole { get; set; }
        public int? Fromaccountid { get; set; }

        public string? Torole { get; set; }        // optional
        public string? Actiontype { get; set; }    // SUBMIT, START_REVIEW, ADMIN_REJECT...
        public string? Message { get; set; }
        public string? Metajson { get; set; }
        public DateTime Createdat { get; set; }

        public virtual Quotation Quotation { get; set; } = null!;
    }
}
