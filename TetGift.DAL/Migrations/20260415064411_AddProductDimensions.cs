using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TetGift.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddProductDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "maxheight",
                table: "product_config",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "maxlength",
                table: "product_config",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "maxwidth",
                table: "product_config",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "height",
                table: "product",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "length",
                table: "product",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "width",
                table: "product",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "maxheight",
                table: "product_config");

            migrationBuilder.DropColumn(
                name: "maxlength",
                table: "product_config");

            migrationBuilder.DropColumn(
                name: "maxwidth",
                table: "product_config");

            migrationBuilder.DropColumn(
                name: "height",
                table: "product");

            migrationBuilder.DropColumn(
                name: "length",
                table: "product");

            migrationBuilder.DropColumn(
                name: "width",
                table: "product");
        }
    }
}
