using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TetGift.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddDescriptionToYourEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bỏ qua cột ImageUrl vì Database đã có rồi để tránh lỗi: column "ImageUrl" already exists
            // migrationBuilder.AddColumn<string>(
            //     name: "ImageUrl",
            //     table: "product",
            //     type: "text",
            //     nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropColumn(
            //     name: "ImageUrl",
            //     table: "product");
        }
    }
}
