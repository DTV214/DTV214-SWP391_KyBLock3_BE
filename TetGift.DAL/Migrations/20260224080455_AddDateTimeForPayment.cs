using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TetGift.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddDateTimeForPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "payment",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "payment");
        }
    }
}
