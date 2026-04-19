using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TetGift.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddVatFieldsToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ActualRevenue",
                table: "orders",
                newName: "actualrevenue");

            migrationBuilder.AlterColumn<decimal>(
                name: "actualrevenue",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "requirevatinvoice",
                table: "orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "vatamount",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "vatcompanyaddress",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vatcompanyname",
                table: "orders",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vatcompanytaxcode",
                table: "orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vatinvoiceemail",
                table: "orders",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "vatrate",
                table: "orders",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "requirevatinvoice",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "vatamount",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "vatcompanyaddress",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "vatcompanyname",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "vatcompanytaxcode",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "vatinvoiceemail",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "vatrate",
                table: "orders");

            migrationBuilder.RenameColumn(
                name: "actualrevenue",
                table: "orders",
                newName: "ActualRevenue");

            migrationBuilder.AlterColumn<decimal>(
                name: "ActualRevenue",
                table: "orders",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);
        }
    }
}
