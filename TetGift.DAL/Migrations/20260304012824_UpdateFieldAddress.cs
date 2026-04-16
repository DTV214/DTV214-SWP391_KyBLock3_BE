using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TetGift.DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFieldAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Customeremail",
                table: "store_location");

            migrationBuilder.DropColumn(
                name: "Customername",
                table: "store_location");
        }
    }
}
