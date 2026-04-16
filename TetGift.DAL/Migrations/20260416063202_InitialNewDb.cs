using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TetGift.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialNewDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_category",
                columns: table => new
                {
                    categoryid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    categoryname = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    isdeleted = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("product_category_pkey", x => x.categoryid);
                });

            migrationBuilder.CreateTable(
                name: "product_config",
                columns: table => new
                {
                    configid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    configname = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    isdeleted = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    suitablesuggestion = table.Column<string>(type: "text", nullable: true),
                    totalunit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    maxlength = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    maxwidth = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    maxheight = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    imageurl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("product_config_pkey", x => x.configid);
                });

            migrationBuilder.CreateTable(
                name: "promotion",
                columns: table => new
                {
                    promotionid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MinPriceToApply = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxDiscountPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LimitedCount = table.Column<int>(type: "integer", nullable: true),
                    UsedCount = table.Column<int>(type: "integer", nullable: true),
                    discountvalue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    expirydate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    isdeleted = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    IsPercentage = table.Column<bool>(type: "boolean", nullable: true),
                    IsLimited = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("promotion_pkey", x => x.promotionid);
                });

            migrationBuilder.CreateTable(
                name: "request_contact",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    is_contacted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("request_contact_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "store_location",
                columns: table => new
                {
                    store_location_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: true),
                    address_line = table.Column<string>(type: "text", nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: false),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    open_hours_text = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("store_location_pkey", x => x.store_location_id);
                });

            migrationBuilder.CreateTable(
                name: "config_detail",
                columns: table => new
                {
                    configdetailid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    configid = table.Column<int>(type: "integer", nullable: true),
                    categoryid = table.Column<int>(type: "integer", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("config_detail_pkey", x => x.configdetailid);
                    table.ForeignKey(
                        name: "config_detail_categoryid_fkey",
                        column: x => x.categoryid,
                        principalTable: "product_category",
                        principalColumn: "categoryid");
                    table.ForeignKey(
                        name: "config_detail_configid_fkey",
                        column: x => x.configid,
                        principalTable: "product_config",
                        principalColumn: "configid");
                });

            migrationBuilder.CreateTable(
                name: "account",
                columns: table => new
                {
                    accountid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    fullname = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    register_otp_expires_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    register_otp_fail_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    register_otp_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    register_otp_verified_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DayCreate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConversationId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("account_pkey", x => x.accountid);
                });

            migrationBuilder.CreateTable(
                name: "account_address",
                columns: table => new
                {
                    account_address_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    accountid = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "text", nullable: true),
                    address_line = table.Column<string>(type: "text", nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Customername = table.Column<string>(type: "text", nullable: true),
                    Customerphone = table.Column<string>(type: "text", nullable: true),
                    Customeremail = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("account_address_pkey", x => x.account_address_id);
                    table.ForeignKey(
                        name: "account_address_accountid_fkey",
                        column: x => x.accountid,
                        principalTable: "account",
                        principalColumn: "accountid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountPromotion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    PromotionId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: true),
                    UsedQuantity = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountPromotion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountPromotion_account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "account",
                        principalColumn: "accountid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountPromotion_promotion_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "promotion",
                        principalColumn: "promotionid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "blog",
                columns: table => new
                {
                    blogid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    accountid = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "text", nullable: true),
                    content = table.Column<string>(type: "text", nullable: true),
                    creationdate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    isdeleted = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    VideoUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("blog_pkey", x => x.blogid);
                    table.ForeignKey(
                        name: "blog_accountid_fkey",
                        column: x => x.accountid,
                        principalTable: "account",
                        principalColumn: "accountid");
                });

            migrationBuilder.CreateTable(
                name: "cart",
                columns: table => new
                {
                    cartid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    accountid = table.Column<int>(type: "integer", nullable: true),
                    totalprice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true, defaultValueSql: "0")
                },
                constraints: table =>
                {
                    table.PrimaryKey("cart_pkey", x => x.cartid);
                    table.ForeignKey(
                        name: "cart_accountid_fkey",
                        column: x => x.accountid,
                        principalTable: "account",
                        principalColumn: "accountid");
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conversations_account_UserId",
                        column: x => x.UserId,
                        principalTable: "account",
                        principalColumn: "accountid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    orderid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    accountid = table.Column<int>(type: "integer", nullable: true),
                    promotionid = table.Column<int>(type: "integer", nullable: true),
                    orderdatetime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    totalprice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    customername = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    customerphone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    customeremail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    customeraddress = table.Column<string>(type: "text", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    isquotation = table.Column<int>(type: "integer", nullable: true),
                    shippeddate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("orders_pkey", x => x.orderid);
                    table.ForeignKey(
                        name: "orders_accountid_fkey",
                        column: x => x.accountid,
                        principalTable: "account",
                        principalColumn: "accountid");
                    table.ForeignKey(
                        name: "orders_promotionid_fkey",
                        column: x => x.promotionid,
                        principalTable: "promotion",
                        principalColumn: "promotionid");
                });

            migrationBuilder.CreateTable(
                name: "product",
                columns: table => new
                {
                    productid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    categoryid = table.Column<int>(type: "integer", nullable: true),
                    configid = table.Column<int>(type: "integer", nullable: true),
                    accountid = table.Column<int>(type: "integer", nullable: true),
                    sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    productname = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ImportPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    unit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    length = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    width = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    height = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("product_pkey", x => x.productid);
                    table.ForeignKey(
                        name: "product_accountid_fkey",
                        column: x => x.accountid,
                        principalTable: "account",
                        principalColumn: "accountid");
                    table.ForeignKey(
                        name: "product_categoryid_fkey",
                        column: x => x.categoryid,
                        principalTable: "product_category",
                        principalColumn: "categoryid");
                    table.ForeignKey(
                        name: "product_configid_fkey",
                        column: x => x.configid,
                        principalTable: "product_config",
                        principalColumn: "configid");
                });

            migrationBuilder.CreateTable(
                name: "wallet",
                columns: table => new
                {
                    walletid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    accountid = table.Column<int>(type: "integer", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "ACTIVE"),
                    createdat = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updatedat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("wallet_pkey", x => x.walletid);
                    table.ForeignKey(
                        name: "wallet_accountid_fkey",
                        column: x => x.accountid,
                        principalTable: "account",
                        principalColumn: "accountid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "feedback",
                columns: table => new
                {
                    feedbackid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    accountid = table.Column<int>(type: "integer", nullable: true),
                    orderid = table.Column<int>(type: "integer", nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: true),
                    comment = table.Column<string>(type: "text", nullable: true),
                    isdeleted = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("feedback_pkey", x => x.feedbackid);
                    table.ForeignKey(
                        name: "feedback_accountid_fkey",
                        column: x => x.accountid,
                        principalTable: "account",
                        principalColumn: "accountid");
                    table.ForeignKey(
                        name: "feedback_orderid_fkey",
                        column: x => x.orderid,
                        principalTable: "orders",
                        principalColumn: "orderid");
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConversationId = table.Column<int>(type: "integer", nullable: false),
                    SenderId = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Messages_account_SenderId",
                        column: x => x.SenderId,
                        principalTable: "account",
                        principalColumn: "accountid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Messages_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "orderid",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "quotation",
                columns: table => new
                {
                    quotationid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    accountid = table.Column<int>(type: "integer", nullable: true),
                    orderid = table.Column<int>(type: "integer", nullable: true),
                    requestdate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    company = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    totalprice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    quotationtype = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    desiredbudget = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    desiredpricenote = table.Column<string>(type: "text", nullable: true),
                    revision = table.Column<int>(type: "integer", nullable: true, defaultValue: 1),
                    submittedat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    staffreviewedat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    adminreviewedat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    customerrespondedat = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    staffreviewerid = table.Column<int>(type: "integer", nullable: true),
                    adminreviewerid = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("quotation_pkey", x => x.quotationid);
                    table.ForeignKey(
                        name: "quotation_accountid_fkey",
                        column: x => x.accountid,
                        principalTable: "account",
                        principalColumn: "accountid");
                    table.ForeignKey(
                        name: "quotation_orderid_fkey",
                        column: x => x.orderid,
                        principalTable: "orders",
                        principalColumn: "orderid");
                });

            migrationBuilder.CreateTable(
                name: "cart_detail",
                columns: table => new
                {
                    cartdetailid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cartid = table.Column<int>(type: "integer", nullable: true),
                    productid = table.Column<int>(type: "integer", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("cart_detail_pkey", x => x.cartdetailid);
                    table.ForeignKey(
                        name: "cart_detail_cartid_fkey",
                        column: x => x.cartid,
                        principalTable: "cart",
                        principalColumn: "cartid");
                    table.ForeignKey(
                        name: "cart_detail_productid_fkey",
                        column: x => x.productid,
                        principalTable: "product",
                        principalColumn: "productid");
                });

            migrationBuilder.CreateTable(
                name: "order_detail",
                columns: table => new
                {
                    orderdetailid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orderid = table.Column<int>(type: "integer", nullable: true),
                    productid = table.Column<int>(type: "integer", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("order_detail_pkey", x => x.orderdetailid);
                    table.ForeignKey(
                        name: "order_detail_orderid_fkey",
                        column: x => x.orderid,
                        principalTable: "orders",
                        principalColumn: "orderid");
                    table.ForeignKey(
                        name: "order_detail_productid_fkey",
                        column: x => x.productid,
                        principalTable: "product",
                        principalColumn: "productid");
                });

            migrationBuilder.CreateTable(
                name: "product_detail",
                columns: table => new
                {
                    productdetailid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    productparentid = table.Column<int>(type: "integer", nullable: true),
                    productid = table.Column<int>(type: "integer", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("product_detail_pkey", x => x.productdetailid);
                    table.ForeignKey(
                        name: "product_detail_productid_fkey",
                        column: x => x.productid,
                        principalTable: "product",
                        principalColumn: "productid");
                    table.ForeignKey(
                        name: "product_detail_productparentid_fkey",
                        column: x => x.productparentid,
                        principalTable: "product",
                        principalColumn: "productid");
                });

            migrationBuilder.CreateTable(
                name: "stock",
                columns: table => new
                {
                    stockid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    productid = table.Column<int>(type: "integer", nullable: true),
                    stockquantity = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    lastupdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    productiondate = table.Column<DateOnly>(type: "date", nullable: true),
                    expirydate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("stock_pkey", x => x.stockid);
                    table.ForeignKey(
                        name: "stock_productid_fkey",
                        column: x => x.productid,
                        principalTable: "product",
                        principalColumn: "productid");
                });

            migrationBuilder.CreateTable(
                name: "payment",
                columns: table => new
                {
                    paymentid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orderid = table.Column<int>(type: "integer", nullable: true),
                    ispayonline = table.Column<bool>(type: "boolean", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    walletid = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    paymentmethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    transactionno = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("payment_pkey", x => x.paymentid);
                    table.ForeignKey(
                        name: "payment_orderid_fkey",
                        column: x => x.orderid,
                        principalTable: "orders",
                        principalColumn: "orderid");
                    table.ForeignKey(
                        name: "payment_walletid_fkey",
                        column: x => x.walletid,
                        principalTable: "wallet",
                        principalColumn: "walletid");
                });

            migrationBuilder.CreateTable(
                name: "wallet_transaction",
                columns: table => new
                {
                    transactionid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    walletid = table.Column<int>(type: "integer", nullable: false),
                    orderid = table.Column<int>(type: "integer", nullable: true),
                    transactiontype = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    balancebefore = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    balanceafter = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "SUCCESS"),
                    description = table.Column<string>(type: "text", nullable: true),
                    createdat = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("wallet_transaction_pkey", x => x.transactionid);
                    table.ForeignKey(
                        name: "wallet_transaction_orderid_fkey",
                        column: x => x.orderid,
                        principalTable: "orders",
                        principalColumn: "orderid",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "wallet_transaction_walletid_fkey",
                        column: x => x.walletid,
                        principalTable: "wallet",
                        principalColumn: "walletid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quotation_category_request",
                columns: table => new
                {
                    quotationcategoryrequestid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    quotationid = table.Column<int>(type: "integer", nullable: false),
                    categoryid = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    createdat = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("quotation_category_request_pkey", x => x.quotationcategoryrequestid);
                    table.ForeignKey(
                        name: "quotation_category_request_categoryid_fkey",
                        column: x => x.categoryid,
                        principalTable: "product_category",
                        principalColumn: "categoryid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "quotation_category_request_quotationid_fkey",
                        column: x => x.quotationid,
                        principalTable: "quotation",
                        principalColumn: "quotationid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quotation_item",
                columns: table => new
                {
                    quotationitemid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    quotationid = table.Column<int>(type: "integer", nullable: true),
                    productid = table.Column<int>(type: "integer", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("quotation_item_pkey", x => x.quotationitemid);
                    table.ForeignKey(
                        name: "quotation_item_productid_fkey",
                        column: x => x.productid,
                        principalTable: "product",
                        principalColumn: "productid");
                    table.ForeignKey(
                        name: "quotation_item_quotationid_fkey",
                        column: x => x.quotationid,
                        principalTable: "quotation",
                        principalColumn: "quotationid");
                });

            migrationBuilder.CreateTable(
                name: "quotation_message",
                columns: table => new
                {
                    quotationmessageid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    quotationid = table.Column<int>(type: "integer", nullable: false),
                    fromrole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    fromaccountid = table.Column<int>(type: "integer", nullable: true),
                    torole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    actiontype = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
                    metajson = table.Column<string>(type: "text", nullable: true),
                    createdat = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("quotation_message_pkey", x => x.quotationmessageid);
                    table.ForeignKey(
                        name: "quotation_message_quotationid_fkey",
                        column: x => x.quotationid,
                        principalTable: "quotation",
                        principalColumn: "quotationid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "custom",
                columns: table => new
                {
                    customid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orderdetailid = table.Column<int>(type: "integer", nullable: true),
                    logourl = table.Column<string>(type: "text", nullable: true),
                    greetingcardtemplate = table.Column<string>(type: "text", nullable: true),
                    greetingcardcontent = table.Column<string>(type: "text", nullable: true),
                    greetingcardcustomurl = table.Column<string>(type: "text", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("custom_pkey", x => x.customid);
                    table.ForeignKey(
                        name: "custom_orderdetailid_fkey",
                        column: x => x.orderdetailid,
                        principalTable: "order_detail",
                        principalColumn: "orderdetailid");
                });

            migrationBuilder.CreateTable(
                name: "stock_movement",
                columns: table => new
                {
                    stockmovementid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stockid = table.Column<int>(type: "integer", nullable: true),
                    orderid = table.Column<int>(type: "integer", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: true),
                    movementdate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("stock_movement_pkey", x => x.stockmovementid);
                    table.ForeignKey(
                        name: "stock_movement_orderid_fkey",
                        column: x => x.orderid,
                        principalTable: "orders",
                        principalColumn: "orderid");
                    table.ForeignKey(
                        name: "stock_movement_stockid_fkey",
                        column: x => x.stockid,
                        principalTable: "stock",
                        principalColumn: "stockid");
                });

            migrationBuilder.CreateTable(
                name: "quotation_fee",
                columns: table => new
                {
                    quotationfeeid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    quotationitemid = table.Column<int>(type: "integer", nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    issubtracted = table.Column<short>(type: "smallint", nullable: true),
                    Quotationid = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("quotation_fee_pkey", x => x.quotationfeeid);
                    table.ForeignKey(
                        name: "FK_quotation_fee_quotation_Quotationid",
                        column: x => x.Quotationid,
                        principalTable: "quotation",
                        principalColumn: "quotationid");
                    table.ForeignKey(
                        name: "quotation_fee_quotationitemid_fkey",
                        column: x => x.quotationitemid,
                        principalTable: "quotation_item",
                        principalColumn: "quotationitemid");
                });

            migrationBuilder.CreateIndex(
                name: "account_username_key",
                table: "account",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_account_ConversationId",
                table: "account",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_account_address_accountid",
                table: "account_address",
                column: "accountid");

            migrationBuilder.CreateIndex(
                name: "IX_AccountPromotion_AccountId",
                table: "AccountPromotion",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountPromotion_PromotionId",
                table: "AccountPromotion",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_blog_accountid",
                table: "blog",
                column: "accountid");

            migrationBuilder.CreateIndex(
                name: "IX_cart_accountid",
                table: "cart",
                column: "accountid");

            migrationBuilder.CreateIndex(
                name: "IX_cart_detail_cartid",
                table: "cart_detail",
                column: "cartid");

            migrationBuilder.CreateIndex(
                name: "IX_cart_detail_productid",
                table: "cart_detail",
                column: "productid");

            migrationBuilder.CreateIndex(
                name: "IX_config_detail_categoryid",
                table: "config_detail",
                column: "categoryid");

            migrationBuilder.CreateIndex(
                name: "IX_config_detail_configid",
                table: "config_detail",
                column: "configid");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_UserId",
                table: "Conversations",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_custom_orderdetailid",
                table: "custom",
                column: "orderdetailid");

            migrationBuilder.CreateIndex(
                name: "IX_feedback_accountid",
                table: "feedback",
                column: "accountid");

            migrationBuilder.CreateIndex(
                name: "IX_feedback_orderid",
                table: "feedback",
                column: "orderid");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId",
                table: "Messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_OrderId",
                table: "Messages",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderId",
                table: "Messages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_order_detail_orderid",
                table: "order_detail",
                column: "orderid");

            migrationBuilder.CreateIndex(
                name: "IX_order_detail_productid",
                table: "order_detail",
                column: "productid");

            migrationBuilder.CreateIndex(
                name: "IX_orders_accountid",
                table: "orders",
                column: "accountid");

            migrationBuilder.CreateIndex(
                name: "IX_orders_promotionid",
                table: "orders",
                column: "promotionid");

            migrationBuilder.CreateIndex(
                name: "IX_payment_orderid",
                table: "payment",
                column: "orderid");

            migrationBuilder.CreateIndex(
                name: "IX_payment_walletid",
                table: "payment",
                column: "walletid");

            migrationBuilder.CreateIndex(
                name: "IX_product_accountid",
                table: "product",
                column: "accountid");

            migrationBuilder.CreateIndex(
                name: "IX_product_categoryid",
                table: "product",
                column: "categoryid");

            migrationBuilder.CreateIndex(
                name: "IX_product_configid",
                table: "product",
                column: "configid");

            migrationBuilder.CreateIndex(
                name: "IX_product_detail_productid",
                table: "product_detail",
                column: "productid");

            migrationBuilder.CreateIndex(
                name: "IX_product_detail_productparentid",
                table: "product_detail",
                column: "productparentid");

            migrationBuilder.CreateIndex(
                name: "promotion_code_key",
                table: "promotion",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quotation_accountid",
                table: "quotation",
                column: "accountid");

            migrationBuilder.CreateIndex(
                name: "IX_quotation_orderid",
                table: "quotation",
                column: "orderid");

            migrationBuilder.CreateIndex(
                name: "IX_quotation_category_request_categoryid",
                table: "quotation_category_request",
                column: "categoryid");

            migrationBuilder.CreateIndex(
                name: "IX_quotation_category_request_quotationid",
                table: "quotation_category_request",
                column: "quotationid");

            migrationBuilder.CreateIndex(
                name: "IX_quotation_fee_Quotationid",
                table: "quotation_fee",
                column: "Quotationid");

            migrationBuilder.CreateIndex(
                name: "IX_quotation_fee_quotationitemid",
                table: "quotation_fee",
                column: "quotationitemid");

            migrationBuilder.CreateIndex(
                name: "IX_quotation_item_productid",
                table: "quotation_item",
                column: "productid");

            migrationBuilder.CreateIndex(
                name: "IX_quotation_item_quotationid",
                table: "quotation_item",
                column: "quotationid");

            migrationBuilder.CreateIndex(
                name: "IX_quotation_message_quotationid",
                table: "quotation_message",
                column: "quotationid");

            migrationBuilder.CreateIndex(
                name: "IX_stock_productid",
                table: "stock",
                column: "productid");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_orderid",
                table: "stock_movement",
                column: "orderid");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_stockid",
                table: "stock_movement",
                column: "stockid");

            migrationBuilder.CreateIndex(
                name: "wallet_accountid_key",
                table: "wallet",
                column: "accountid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transaction_orderid",
                table: "wallet_transaction",
                column: "orderid");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transaction_walletid",
                table: "wallet_transaction",
                column: "walletid");

            migrationBuilder.AddForeignKey(
                name: "FK_account_Conversations_ConversationId",
                table: "account",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_account_Conversations_ConversationId",
                table: "account");

            migrationBuilder.DropTable(
                name: "account_address");

            migrationBuilder.DropTable(
                name: "AccountPromotion");

            migrationBuilder.DropTable(
                name: "blog");

            migrationBuilder.DropTable(
                name: "cart_detail");

            migrationBuilder.DropTable(
                name: "config_detail");

            migrationBuilder.DropTable(
                name: "custom");

            migrationBuilder.DropTable(
                name: "feedback");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "payment");

            migrationBuilder.DropTable(
                name: "product_detail");

            migrationBuilder.DropTable(
                name: "quotation_category_request");

            migrationBuilder.DropTable(
                name: "quotation_fee");

            migrationBuilder.DropTable(
                name: "quotation_message");

            migrationBuilder.DropTable(
                name: "request_contact");

            migrationBuilder.DropTable(
                name: "stock_movement");

            migrationBuilder.DropTable(
                name: "store_location");

            migrationBuilder.DropTable(
                name: "wallet_transaction");

            migrationBuilder.DropTable(
                name: "cart");

            migrationBuilder.DropTable(
                name: "order_detail");

            migrationBuilder.DropTable(
                name: "quotation_item");

            migrationBuilder.DropTable(
                name: "stock");

            migrationBuilder.DropTable(
                name: "wallet");

            migrationBuilder.DropTable(
                name: "quotation");

            migrationBuilder.DropTable(
                name: "product");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "product_category");

            migrationBuilder.DropTable(
                name: "product_config");

            migrationBuilder.DropTable(
                name: "promotion");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "account");
        }
    }
}
