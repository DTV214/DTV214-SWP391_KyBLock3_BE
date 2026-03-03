using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TetGift.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddIsQuotationToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "isquotation",
                table: "orders",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isquotation",
                table: "orders");
        }
    }
}
