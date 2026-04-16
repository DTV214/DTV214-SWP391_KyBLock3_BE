using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TetGift.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "paymentmethod",
                table: "payment",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "transactionno",
                table: "payment",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "walletid",
                table: "payment",
                type: "integer",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_payment_walletid",
                table: "payment",
                column: "walletid");

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
                name: "payment_walletid_fkey",
                table: "payment",
                column: "walletid",
                principalTable: "wallet",
                principalColumn: "walletid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "payment_walletid_fkey",
                table: "payment");

            migrationBuilder.DropTable(
                name: "wallet_transaction");

            migrationBuilder.DropTable(
                name: "wallet");

            migrationBuilder.DropIndex(
                name: "IX_payment_walletid",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "paymentmethod",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "transactionno",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "walletid",
                table: "payment");
        }
    }
}
