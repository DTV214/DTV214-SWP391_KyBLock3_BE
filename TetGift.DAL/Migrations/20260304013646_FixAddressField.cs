using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TetGift.DAL.Migrations
{
    /// <inheritdoc />
    public partial class FixAddressField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Customeremail",
                table: "store_location");

            migrationBuilder.DropColumn(
                name: "Customername",
                table: "store_location");

            migrationBuilder.AddColumn<string>(
                name: "Customeremail",
                table: "account_address",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Customername",
                table: "account_address",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Customerphone",
                table: "account_address",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Customeremail",
                table: "account_address");

            migrationBuilder.DropColumn(
                name: "Customername",
                table: "account_address");

            migrationBuilder.DropColumn(
                name: "Customerphone",
                table: "account_address");

            migrationBuilder.AddColumn<string>(
                name: "Customeremail",
                table: "store_location",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Customername",
                table: "store_location",
                type: "text",
                nullable: true);
        }
    }
}
