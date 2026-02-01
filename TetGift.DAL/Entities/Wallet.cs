using System;
using System.Collections.Generic;

namespace TetGift.DAL.Entities;

public partial class Wallet
{
    public int Walletid { get; set; }

    public int Accountid { get; set; }

    public decimal Balance { get; set; }

    public string Status { get; set; } = null!;

    public DateTime Createdat { get; set; }

    public DateTime? Updatedat { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
