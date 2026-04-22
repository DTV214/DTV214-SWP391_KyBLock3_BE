using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TetGift.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddVatFieldsToQuotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "requirevatinvoice",
                table: "quotation",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "vatcompanyaddress",
                table: "quotation",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vatcompanyname",
                table: "quotation",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vatcompanytaxcode",
                table: "quotation",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vatinvoiceemail",
                table: "quotation",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "requirevatinvoice",
                table: "quotation");

            migrationBuilder.DropColumn(
                name: "vatcompanyaddress",
                table: "quotation");

            migrationBuilder.DropColumn(
                name: "vatcompanyname",
                table: "quotation");

            migrationBuilder.DropColumn(
                name: "vatcompanytaxcode",
                table: "quotation");

            migrationBuilder.DropColumn(
                name: "vatinvoiceemail",
                table: "quotation");
        }
    }
}
