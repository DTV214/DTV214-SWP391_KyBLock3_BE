using System;
using System.Collections.Generic;

namespace TetGift.DAL.Entities;

public partial class Order
{
    public int Orderid { get; set; }

    public int? Accountid { get; set; }

    public int? Promotionid { get; set; }

    public DateTime? Orderdatetime { get; set; }

    public decimal? Totalprice { get; set; }

    public string? Status { get; set; }

    public string? Customername { get; set; }

    public string? Customerphone { get; set; }

    public string? Customeremail { get; set; }

    public string? Customeraddress { get; set; }

    public string? Note { get; set; }

    public virtual Account? Account { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Promotion? Promotion { get; set; }

    public virtual ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
