using System;
using System.Collections.Generic;

namespace TetGift.DAL.Entities;

public partial class Quotation
{
    public int Quotationid { get; set; }

    public int? Accountid { get; set; }

    public int? Orderid { get; set; }

    public DateTime? Requestdate { get; set; }

    public string? Status { get; set; }

    public string? Company { get; set; }

    public string? Address { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public decimal? Totalprice { get; set; }

    public string? Note { get; set; }

    //New
    public string? Quotationtype { get; set; }           // "MANUAL" | "BUDGET_RECOMMEND"
    public decimal? Desiredbudget { get; set; }          // for flow 2
    public string? Desiredpricenote { get; set; }        // user desired price note
    public int? Revision { get; set; }                   // default 1

    public DateTime? Submittedat { get; set; }
    public DateTime? Staffreviewedat { get; set; }
    public DateTime? Adminreviewedat { get; set; }
    public DateTime? Customerrespondedat { get; set; }

    public int? Staffreviewerid { get; set; }
    public int? Adminreviewerid { get; set; }

    public virtual Account? Account { get; set; }

    public virtual Order? Order { get; set; }

    public virtual ICollection<QuotationItem> QuotationItems { get; set; } = new List<QuotationItem>();

    public virtual ICollection<QuotationFee> QuotationFees { get; set; } = new List<QuotationFee>(); // optional if you want direct
    public virtual ICollection<QuotationMessage> QuotationMessages { get; set; } = new List<QuotationMessage>();
    public virtual ICollection<QuotationCategoryRequest> QuotationCategoryRequests { get; set; } = new List<QuotationCategoryRequest>();
}
