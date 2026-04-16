using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TetGift.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddNewQuotesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Quotationid",
                table: "quotation_fee",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "adminreviewedat",
                table: "quotation",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "adminreviewerid",
                table: "quotation",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "customerrespondedat",
                table: "quotation",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "desiredbudget",
                table: "quotation",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "desiredpricenote",
                table: "quotation",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "quotationtype",
                table: "quotation",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "revision",
                table: "quotation",
                type: "integer",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "staffreviewedat",
                table: "quotation",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "staffreviewerid",
                table: "quotation",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "submittedat",
                table: "quotation",
                type: "timestamp without time zone",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_quotation_fee_Quotationid",
                table: "quotation_fee",
                column: "Quotationid");

            migrationBuilder.CreateIndex(
                name: "IX_quotation_category_request_categoryid",
                table: "quotation_category_request",
                column: "categoryid");

            migrationBuilder.CreateIndex(
                name: "IX_quotation_category_request_quotationid",
                table: "quotation_category_request",
                column: "quotationid");

            migrationBuilder.CreateIndex(
                name: "IX_quotation_message_quotationid",
                table: "quotation_message",
                column: "quotationid");

            migrationBuilder.AddForeignKey(
                name: "FK_quotation_fee_quotation_Quotationid",
                table: "quotation_fee",
                column: "Quotationid",
                principalTable: "quotation",
                principalColumn: "quotationid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_quotation_fee_quotation_Quotationid",
                table: "quotation_fee");

            migrationBuilder.DropTable(
                name: "quotation_category_request");

            migrationBuilder.DropTable(
                name: "quotation_message");

            migrationBuilder.DropIndex(
                name: "IX_quotation_fee_Quotationid",
                table: "quotation_fee");

            migrationBuilder.DropColumn(
                name: "Quotationid",
                table: "quotation_fee");

            migrationBuilder.DropColumn(
                name: "adminreviewedat",
                table: "quotation");

            migrationBuilder.DropColumn(
                name: "adminreviewerid",
                table: "quotation");

            migrationBuilder.DropColumn(
                name: "customerrespondedat",
                table: "quotation");

            migrationBuilder.DropColumn(
                name: "desiredbudget",
                table: "quotation");

            migrationBuilder.DropColumn(
                name: "desiredpricenote",
                table: "quotation");

            migrationBuilder.DropColumn(
                name: "quotationtype",
                table: "quotation");

            migrationBuilder.DropColumn(
                name: "revision",
                table: "quotation");

            migrationBuilder.DropColumn(
                name: "staffreviewedat",
                table: "quotation");

            migrationBuilder.DropColumn(
                name: "staffreviewerid",
                table: "quotation");

            migrationBuilder.DropColumn(
                name: "submittedat",
                table: "quotation");
        }
    }
}
