using System;
using System.Collections.Generic;

namespace TetGift.DAL.Entities;

public partial class Payment
{
    public int Paymentid { get; set; }

    public int? Orderid { get; set; }

    public bool? Ispayonline { get; set; }

    public decimal? Amount { get; set; }

    public string? Status { get; set; }

    public string? Type { get; set; }

    public int? Walletid { get; set; }

    public string? Paymentmethod { get; set; } // VNPAY, WALLET

    public string? Transactionno { get; set; }

    public virtual Order? Order { get; set; }

    public virtual Wallet? Wallet { get; set; }
}
