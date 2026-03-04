using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TetGift.DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePromo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "promotion",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLimited",
                table: "promotion",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPercentage",
                table: "promotion",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LimitedCount",
                table: "promotion",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDiscountPrice",
                table: "promotion",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinPriceToApply",
                table: "promotion",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartTime",
                table: "promotion",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsedCount",
                table: "promotion",
                type: "integer",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_AccountPromotion_AccountId",
                table: "AccountPromotion",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountPromotion_PromotionId",
                table: "AccountPromotion",
                column: "PromotionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountPromotion");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "promotion");

            migrationBuilder.DropColumn(
                name: "IsLimited",
                table: "promotion");

            migrationBuilder.DropColumn(
                name: "IsPercentage",
                table: "promotion");

            migrationBuilder.DropColumn(
                name: "LimitedCount",
                table: "promotion");

            migrationBuilder.DropColumn(
                name: "MaxDiscountPrice",
                table: "promotion");

            migrationBuilder.DropColumn(
                name: "MinPriceToApply",
                table: "promotion");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "promotion");

            migrationBuilder.DropColumn(
                name: "UsedCount",
                table: "promotion");
        }
    }
}
