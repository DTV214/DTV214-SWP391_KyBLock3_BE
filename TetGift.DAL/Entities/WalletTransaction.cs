using System;

namespace TetGift.DAL.Entities;

public partial class WalletTransaction
{
    public int Transactionid { get; set; }

    public int Walletid { get; set; }

    public int? Orderid { get; set; }

    public string Transactiontype { get; set; } = null!; // DEPOSIT, PAYMENT, REFUND

    public decimal Amount { get; set; }

    public decimal Balancebefore { get; set; }

    public decimal Balanceafter { get; set; }

    public string Status { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime Createdat { get; set; }

    public virtual Wallet Wallet { get; set; } = null!;

    public virtual Order? Order { get; set; }
}
