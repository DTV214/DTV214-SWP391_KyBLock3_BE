using System;
using System.Collections.Generic;

namespace TetGift.DAL.Entities;

public partial class Account
{
    public int Accountid { get; set; }

    public string? Role { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? Email { get; set; }

    public string? Fullname { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? Status { get; set; }

    public DateTime? RegisterOtpExpiresAt { get; set; }

    public int RegisterOtpFailCount { get; set; }

    public string? RegisterOtpHash { get; set; }

    public DateTime? RegisterOtpVerifiedAt { get; set; }

    public virtual ICollection<Blog> Blogs { get; set; } = new List<Blog>();

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();

    public virtual Wallet? Wallet { get; set; }
}
