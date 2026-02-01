using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TetGift.DAL.Entities;

namespace TetGift.DAL.Context;

public partial class DatabaseContext : DbContext
{
    public DatabaseContext()
    {
    }

    public DatabaseContext(DbContextOptions<DatabaseContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Blog> Blogs { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartDetail> CartDetails { get; set; }

    public virtual DbSet<ConfigDetail> ConfigDetails { get; set; }

    public virtual DbSet<Custom> Customs { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductCategory> ProductCategories { get; set; }

    public virtual DbSet<ProductConfig> ProductConfigs { get; set; }

    public virtual DbSet<ProductDetail> ProductDetails { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<Quotation> Quotations { get; set; }

    public virtual DbSet<QuotationFee> QuotationFees { get; set; }

    public virtual DbSet<QuotationItem> QuotationItems { get; set; }

    public virtual DbSet<Stock> Stocks { get; set; }

    public virtual DbSet<StockMovement> StockMovements { get; set; }

    public virtual DbSet<QuotationMessage> QuotationMessages { get; set; }

    public virtual DbSet<QuotationCategoryRequest> QuotationCategoryRequests { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    public virtual DbSet<WalletTransaction> WalletTransactions { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("auth", "aal_level", new[] { "aal1", "aal2", "aal3" })
            .HasPostgresEnum("auth", "code_challenge_method", new[] { "s256", "plain" })
            .HasPostgresEnum("auth", "factor_status", new[] { "unverified", "verified" })
            .HasPostgresEnum("auth", "factor_type", new[] { "totp", "webauthn", "phone" })
            .HasPostgresEnum("auth", "oauth_authorization_status", new[] { "pending", "approved", "denied", "expired" })
            .HasPostgresEnum("auth", "oauth_client_type", new[] { "public", "confidential" })
            .HasPostgresEnum("auth", "oauth_registration_type", new[] { "dynamic", "manual" })
            .HasPostgresEnum("auth", "oauth_response_type", new[] { "code" })
            .HasPostgresEnum("auth", "one_time_token_type", new[] { "confirmation_token", "reauthentication_token", "recovery_token", "email_change_token_new", "email_change_token_current", "phone_change_token" })
            .HasPostgresEnum("realtime", "action", new[] { "INSERT", "UPDATE", "DELETE", "TRUNCATE", "ERROR" })
            .HasPostgresEnum("realtime", "equality_op", new[] { "eq", "neq", "lt", "lte", "gt", "gte", "in" })
            .HasPostgresEnum("storage", "buckettype", new[] { "STANDARD", "ANALYTICS", "VECTOR" })
            .HasPostgresExtension("extensions", "pg_stat_statements")
            .HasPostgresExtension("extensions", "pgcrypto")
            .HasPostgresExtension("extensions", "uuid-ossp")
            .HasPostgresExtension("graphql", "pg_graphql")
            .HasPostgresExtension("vault", "supabase_vault");

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Accountid).HasName("account_pkey");

            entity.ToTable("account");

            entity.HasIndex(e => e.Username, "account_username_key").IsUnique();

            entity.Property(e => e.Accountid).HasColumnName("accountid");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Fullname)
                .HasMaxLength(255)
                .HasColumnName("fullname");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.RegisterOtpExpiresAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("register_otp_expires_at");
            entity.Property(e => e.RegisterOtpFailCount)
                .HasDefaultValue(0)
                .HasColumnName("register_otp_fail_count");
            entity.Property(e => e.RegisterOtpHash)
                .HasMaxLength(255)
                .HasColumnName("register_otp_hash");
            entity.Property(e => e.RegisterOtpVerifiedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("register_otp_verified_at");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasColumnName("role");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .HasColumnName("username");
        });

        modelBuilder.Entity<Blog>(entity =>
        {
            entity.HasKey(e => e.Blogid).HasName("blog_pkey");

            entity.ToTable("blog");

            entity.Property(e => e.Blogid).HasColumnName("blogid");
            entity.Property(e => e.Accountid).HasColumnName("accountid");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.Creationdate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("creationdate");
            entity.Property(e => e.Isdeleted)
                .HasDefaultValue(false)
                .HasColumnName("isdeleted");
            entity.Property(e => e.Title).HasColumnName("title");

            entity.HasOne(d => d.Account).WithMany(p => p.Blogs)
                .HasForeignKey(d => d.Accountid)
                .HasConstraintName("blog_accountid_fkey");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Cartid).HasName("cart_pkey");

            entity.ToTable("cart");

            entity.Property(e => e.Cartid).HasColumnName("cartid");
            entity.Property(e => e.Accountid).HasColumnName("accountid");
            entity.Property(e => e.Totalprice)
                .HasPrecision(18, 2)
                .HasDefaultValueSql("0")
                .HasColumnName("totalprice");

            entity.HasOne(d => d.Account).WithMany(p => p.Carts)
                .HasForeignKey(d => d.Accountid)
                .HasConstraintName("cart_accountid_fkey");
        });

        modelBuilder.Entity<CartDetail>(entity =>
        {
            entity.HasKey(e => e.Cartdetailid).HasName("cart_detail_pkey");

            entity.ToTable("cart_detail");

            entity.Property(e => e.Cartdetailid).HasColumnName("cartdetailid");
            entity.Property(e => e.Cartid).HasColumnName("cartid");
            entity.Property(e => e.Productid).HasColumnName("productid");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Cart).WithMany(p => p.CartDetails)
                .HasForeignKey(d => d.Cartid)
                .HasConstraintName("cart_detail_cartid_fkey");

            entity.HasOne(d => d.Product).WithMany(p => p.CartDetails)
                .HasForeignKey(d => d.Productid)
                .HasConstraintName("cart_detail_productid_fkey");
        });

        modelBuilder.Entity<ConfigDetail>(entity =>
        {
            entity.HasKey(e => e.Configdetailid).HasName("config_detail_pkey");

            entity.ToTable("config_detail");

            entity.Property(e => e.Configdetailid).HasColumnName("configdetailid");
            entity.Property(e => e.Categoryid).HasColumnName("categoryid");
            entity.Property(e => e.Configid).HasColumnName("configid");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Category).WithMany(p => p.ConfigDetails)
                .HasForeignKey(d => d.Categoryid)
                .HasConstraintName("config_detail_categoryid_fkey");

            entity.HasOne(d => d.Config).WithMany(p => p.ConfigDetails)
                .HasForeignKey(d => d.Configid)
                .HasConstraintName("config_detail_configid_fkey");
        });

        modelBuilder.Entity<Custom>(entity =>
        {
            entity.HasKey(e => e.Customid).HasName("custom_pkey");

            entity.ToTable("custom");

            entity.Property(e => e.Customid).HasColumnName("customid");
            entity.Property(e => e.Greetingcardcontent).HasColumnName("greetingcardcontent");
            entity.Property(e => e.Greetingcardcustomurl).HasColumnName("greetingcardcustomurl");
            entity.Property(e => e.Greetingcardtemplate).HasColumnName("greetingcardtemplate");
            entity.Property(e => e.Logourl).HasColumnName("logourl");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.Orderdetailid).HasColumnName("orderdetailid");

            entity.HasOne(d => d.Orderdetail).WithMany(p => p.Customs)
                .HasForeignKey(d => d.Orderdetailid)
                .HasConstraintName("custom_orderdetailid_fkey");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.Feedbackid).HasName("feedback_pkey");

            entity.ToTable("feedback");

            entity.Property(e => e.Feedbackid).HasColumnName("feedbackid");
            entity.Property(e => e.Accountid).HasColumnName("accountid");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.Isdeleted)
                .HasDefaultValue(false)
                .HasColumnName("isdeleted");
            entity.Property(e => e.Orderid).HasColumnName("orderid");
            entity.Property(e => e.Rating).HasColumnName("rating");

            entity.HasOne(d => d.Account).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.Accountid)
                .HasConstraintName("feedback_accountid_fkey");

            entity.HasOne(d => d.Order).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.Orderid)
                .HasConstraintName("feedback_orderid_fkey");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Orderid).HasName("orders_pkey");

            entity.ToTable("orders");

            entity.Property(e => e.Orderid).HasColumnName("orderid");
            entity.Property(e => e.Accountid).HasColumnName("accountid");
            entity.Property(e => e.Customeraddress).HasColumnName("customeraddress");
            entity.Property(e => e.Customeremail)
                .HasMaxLength(255)
                .HasColumnName("customeremail");
            entity.Property(e => e.Customername)
                .HasMaxLength(255)
                .HasColumnName("customername");
            entity.Property(e => e.Customerphone)
                .HasMaxLength(20)
                .HasColumnName("customerphone");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.Orderdatetime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("orderdatetime");
            entity.Property(e => e.Promotionid).HasColumnName("promotionid");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Totalprice)
                .HasPrecision(18, 2)
                .HasColumnName("totalprice");

            entity.HasOne(d => d.Account).WithMany(p => p.Orders)
                .HasForeignKey(d => d.Accountid)
                .HasConstraintName("orders_accountid_fkey");

            entity.HasOne(d => d.Promotion).WithMany(p => p.Orders)
                .HasForeignKey(d => d.Promotionid)
                .HasConstraintName("orders_promotionid_fkey");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.Orderdetailid).HasName("order_detail_pkey");

            entity.ToTable("order_detail");

            entity.Property(e => e.Orderdetailid).HasColumnName("orderdetailid");
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .HasColumnName("amount");
            entity.Property(e => e.Orderid).HasColumnName("orderid");
            entity.Property(e => e.Productid).HasColumnName("productid");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.Orderid)
                .HasConstraintName("order_detail_orderid_fkey");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.Productid)
                .HasConstraintName("order_detail_productid_fkey");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Paymentid).HasName("payment_pkey");

            entity.ToTable("payment");

            entity.Property(e => e.Paymentid).HasColumnName("paymentid");
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .HasColumnName("amount");
            entity.Property(e => e.Ispayonline).HasColumnName("ispayonline");
            entity.Property(e => e.Orderid).HasColumnName("orderid");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
            entity.Property(e => e.Walletid).HasColumnName("walletid");
            entity.Property(e => e.Paymentmethod)
                .HasMaxLength(50)
                .HasColumnName("paymentmethod");
            entity.Property(e => e.Transactionno)
                .HasMaxLength(255)
                .HasColumnName("transactionno");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.Orderid)
                .HasConstraintName("payment_orderid_fkey");

            entity.HasOne(d => d.Wallet).WithMany(p => p.Payments)
                .HasForeignKey(d => d.Walletid)
                .HasConstraintName("payment_walletid_fkey");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Productid).HasName("product_pkey");

            entity.ToTable("product");

            entity.Property(e => e.Productid).HasColumnName("productid");
            entity.Property(e => e.Accountid).HasColumnName("accountid");
            entity.Property(e => e.Categoryid).HasColumnName("categoryid");
            entity.Property(e => e.Configid).HasColumnName("configid");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Price)
                .HasPrecision(18, 2)
                .HasColumnName("price");
            entity.Property(e => e.Productname)
                .HasMaxLength(255)
                .HasColumnName("productname");
            entity.Property(e => e.Sku)
                .HasMaxLength(100)
                .HasColumnName("sku");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Unit)
                .HasPrecision(18, 2)
                .HasColumnName("unit");

            entity.HasOne(d => d.Account).WithMany(p => p.Products)
                .HasForeignKey(d => d.Accountid)
                .HasConstraintName("product_accountid_fkey");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.Categoryid)
                .HasConstraintName("product_categoryid_fkey");

            entity.HasOne(d => d.Config).WithMany(p => p.Products)
                .HasForeignKey(d => d.Configid)
                .HasConstraintName("product_configid_fkey");
        });

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.HasKey(e => e.Categoryid).HasName("product_category_pkey");

            entity.ToTable("product_category");

            entity.Property(e => e.Categoryid).HasColumnName("categoryid");
            entity.Property(e => e.Categoryname)
                .HasMaxLength(255)
                .HasColumnName("categoryname");
            entity.Property(e => e.Isdeleted)
                .HasDefaultValue(false)
                .HasColumnName("isdeleted");
        });

        modelBuilder.Entity<ProductConfig>(entity =>
        {
            entity.HasKey(e => e.Configid).HasName("product_config_pkey");

            entity.ToTable("product_config");

            entity.Property(e => e.Configid).HasColumnName("configid");
            entity.Property(e => e.Configname)
                .HasMaxLength(255)
                .HasColumnName("configname");
            entity.Property(e => e.Imageurl).HasColumnName("imageurl");
            entity.Property(e => e.Isdeleted)
                .HasDefaultValue(false)
                .HasColumnName("isdeleted");
            entity.Property(e => e.Suitablesuggestion).HasColumnName("suitablesuggestion");
            entity.Property(e => e.Totalunit)
                .HasPrecision(18, 2)
                .HasColumnName("totalunit");
        });

        modelBuilder.Entity<ProductDetail>(entity =>
        {
            entity.HasKey(e => e.Productdetailid).HasName("product_detail_pkey");

            entity.ToTable("product_detail");

            entity.Property(e => e.Productdetailid).HasColumnName("productdetailid");
            entity.Property(e => e.Productid).HasColumnName("productid");
            entity.Property(e => e.Productparentid).HasColumnName("productparentid");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductDetailProducts)
                .HasForeignKey(d => d.Productid)
                .HasConstraintName("product_detail_productid_fkey");

            entity.HasOne(d => d.Productparent).WithMany(p => p.ProductDetailProductparents)
                .HasForeignKey(d => d.Productparentid)
                .HasConstraintName("product_detail_productparentid_fkey");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.Promotionid).HasName("promotion_pkey");

            entity.ToTable("promotion");

            entity.HasIndex(e => e.Code, "promotion_code_key").IsUnique();

            entity.Property(e => e.Promotionid).HasColumnName("promotionid");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.Discountvalue)
                .HasPrecision(18, 2)
                .HasColumnName("discountvalue");
            entity.Property(e => e.Expirydate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expirydate");
            entity.Property(e => e.Isdeleted)
                .HasDefaultValue(false)
                .HasColumnName("isdeleted");
        });

        modelBuilder.Entity<Quotation>(entity =>
        {
            entity.HasKey(e => e.Quotationid).HasName("quotation_pkey");

            entity.ToTable("quotation");

            entity.Property(e => e.Quotationid).HasColumnName("quotationid");
            entity.Property(e => e.Accountid).HasColumnName("accountid");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.Company)
                .HasMaxLength(255)
                .HasColumnName("company");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.Orderid).HasColumnName("orderid");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Requestdate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("requestdate");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Totalprice)
                .HasPrecision(18, 2)
                .HasColumnName("totalprice");
            entity.Property(e => e.Quotationtype)
                .HasMaxLength(50)
                .HasColumnName("quotationtype");

            entity.Property(e => e.Desiredbudget)
                .HasPrecision(18, 2)
                .HasColumnName("desiredbudget");

            entity.Property(e => e.Desiredpricenote)
                .HasColumnName("desiredpricenote");

            entity.Property(e => e.Revision)
                .HasDefaultValue(1)
                .HasColumnName("revision");

            entity.Property(e => e.Submittedat)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("submittedat");

            entity.Property(e => e.Staffreviewedat)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("staffreviewedat");

            entity.Property(e => e.Adminreviewedat)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("adminreviewedat");

            entity.Property(e => e.Customerrespondedat)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("customerrespondedat");

            entity.Property(e => e.Staffreviewerid)
                .HasColumnName("staffreviewerid");

            entity.Property(e => e.Adminreviewerid)
                .HasColumnName("adminreviewerid");


            entity.HasOne(d => d.Account).WithMany(p => p.Quotations)
                .HasForeignKey(d => d.Accountid)
                .HasConstraintName("quotation_accountid_fkey");

            entity.HasOne(d => d.Order).WithMany(p => p.Quotations)
                .HasForeignKey(d => d.Orderid)
                .HasConstraintName("quotation_orderid_fkey");
        });

        modelBuilder.Entity<QuotationFee>(entity =>
        {
            entity.HasKey(e => e.Quotationfeeid).HasName("quotation_fee_pkey");

            entity.ToTable("quotation_fee");

            entity.Property(e => e.Quotationfeeid).HasColumnName("quotationfeeid");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Issubtracted).HasColumnName("issubtracted");
            entity.Property(e => e.Price)
                .HasPrecision(18, 2)
                .HasColumnName("price");
            entity.Property(e => e.Quotationitemid).HasColumnName("quotationitemid");

            entity.HasOne(d => d.Quotationitem).WithMany(p => p.QuotationFees)
                .HasForeignKey(d => d.Quotationitemid)
                .HasConstraintName("quotation_fee_quotationitemid_fkey");
        });

        modelBuilder.Entity<QuotationItem>(entity =>
        {
            entity.HasKey(e => e.Quotationitemid).HasName("quotation_item_pkey");

            entity.ToTable("quotation_item");

            entity.Property(e => e.Quotationitemid).HasColumnName("quotationitemid");
            entity.Property(e => e.Price)
                .HasPrecision(18, 2)
                .HasColumnName("price");
            entity.Property(e => e.Productid).HasColumnName("productid");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Quotationid).HasColumnName("quotationid");

            entity.HasOne(d => d.Product).WithMany(p => p.QuotationItems)
                .HasForeignKey(d => d.Productid)
                .HasConstraintName("quotation_item_productid_fkey");

            entity.HasOne(d => d.Quotation).WithMany(p => p.QuotationItems)
                .HasForeignKey(d => d.Quotationid)
                .HasConstraintName("quotation_item_quotationid_fkey");
        });

        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasKey(e => e.Stockid).HasName("stock_pkey");

            entity.ToTable("stock");

            entity.Property(e => e.Stockid).HasColumnName("stockid");
            entity.Property(e => e.Expirydate).HasColumnName("expirydate");
            entity.Property(e => e.Lastupdated)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("lastupdated");
            entity.Property(e => e.Productid).HasColumnName("productid");
            entity.Property(e => e.Productiondate).HasColumnName("productiondate");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Stockquantity).HasColumnName("stockquantity");

            entity.HasOne(d => d.Product).WithMany(p => p.Stocks)
                .HasForeignKey(d => d.Productid)
                .HasConstraintName("stock_productid_fkey");
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.Stockmovementid).HasName("stock_movement_pkey");

            entity.ToTable("stock_movement");

            entity.Property(e => e.Stockmovementid).HasColumnName("stockmovementid");
            entity.Property(e => e.Movementdate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("movementdate");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.Orderid).HasColumnName("orderid");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Stockid).HasColumnName("stockid");

            entity.HasOne(d => d.Order).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.Orderid)
                .HasConstraintName("stock_movement_orderid_fkey");

            entity.HasOne(d => d.Stock).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.Stockid)
                .HasConstraintName("stock_movement_stockid_fkey");
        });

        modelBuilder.Entity<QuotationMessage>(entity =>
        {
            entity.HasKey(e => e.Quotationmessageid).HasName("quotation_message_pkey");
            entity.ToTable("quotation_message");

            entity.Property(e => e.Quotationmessageid).HasColumnName("quotationmessageid");
            entity.Property(e => e.Quotationid).HasColumnName("quotationid");

            entity.Property(e => e.Fromrole)
                .HasMaxLength(50)
                .HasColumnName("fromrole");

            entity.Property(e => e.Fromaccountid).HasColumnName("fromaccountid");

            entity.Property(e => e.Torole)
                .HasMaxLength(50)
                .HasColumnName("torole");

            entity.Property(e => e.Actiontype)
                .HasMaxLength(50)
                .HasColumnName("actiontype");

            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.Metajson).HasColumnName("metajson");

            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createdat");

            entity.HasOne(d => d.Quotation).WithMany(p => p.QuotationMessages)
                .HasForeignKey(d => d.Quotationid)
                .HasConstraintName("quotation_message_quotationid_fkey");
        });

        modelBuilder.Entity<QuotationCategoryRequest>(entity =>
        {
            entity.HasKey(e => e.Quotationcategoryrequestid).HasName("quotation_category_request_pkey");
            entity.ToTable("quotation_category_request");

            entity.Property(e => e.Quotationcategoryrequestid).HasColumnName("quotationcategoryrequestid");
            entity.Property(e => e.Quotationid).HasColumnName("quotationid");
            entity.Property(e => e.Categoryid).HasColumnName("categoryid");

            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Note).HasColumnName("note");

            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createdat");

            entity.HasOne(d => d.Quotation).WithMany(p => p.QuotationCategoryRequests)
                .HasForeignKey(d => d.Quotationid)
                .HasConstraintName("quotation_category_request_quotationid_fkey");

            entity.HasOne(d => d.Category).WithMany()
                .HasForeignKey(d => d.Categoryid)
                .HasConstraintName("quotation_category_request_categoryid_fkey");
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.Walletid).HasName("wallet_pkey");

            entity.ToTable("wallet");

            entity.HasIndex(e => e.Accountid, "wallet_accountid_key").IsUnique();

            entity.Property(e => e.Walletid).HasColumnName("walletid");
            entity.Property(e => e.Accountid).HasColumnName("accountid");
            entity.Property(e => e.Balance)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m)
                .HasColumnName("balance");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createdat");
            entity.Property(e => e.Updatedat)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updatedat");

            entity.HasOne(d => d.Account)
                .WithOne(a => a.Wallet)
                .HasForeignKey<Wallet>(d => d.Accountid)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("wallet_accountid_fkey");
        });

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(e => e.Transactionid).HasName("wallet_transaction_pkey");

            entity.ToTable("wallet_transaction");

            entity.Property(e => e.Transactionid).HasColumnName("transactionid");
            entity.Property(e => e.Walletid).HasColumnName("walletid");
            entity.Property(e => e.Orderid).HasColumnName("orderid");
            entity.Property(e => e.Transactiontype)
                .HasMaxLength(50)
                .HasColumnName("transactiontype");
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .HasColumnName("amount");
            entity.Property(e => e.Balancebefore)
                .HasPrecision(18, 2)
                .HasColumnName("balancebefore");
            entity.Property(e => e.Balanceafter)
                .HasPrecision(18, 2)
                .HasColumnName("balanceafter");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("SUCCESS")
                .HasColumnName("status");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createdat");

            entity.HasOne(d => d.Wallet).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.Walletid)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("wallet_transaction_walletid_fkey");

            entity.HasOne(d => d.Order)
                .WithMany(o => o.WalletTransactions)
                .HasForeignKey(d => d.Orderid)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("wallet_transaction_orderid_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
