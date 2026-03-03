using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TetGift.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddMapEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_address",
                columns: table => new
                {
                    account_address_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    accountid = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "text", nullable: true),
                    address_line = table.Column<string>(type: "text", nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("account_address_pkey", x => x.account_address_id);
                    table.ForeignKey(
                        name: "account_address_accountid_fkey",
                        column: x => x.accountid,
                        principalTable: "account",
                        principalColumn: "accountid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "store_location",
                columns: table => new
                {
                    store_location_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: true),
                    address_line = table.Column<string>(type: "text", nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: false),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    open_hours_text = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("store_location_pkey", x => x.store_location_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_address_accountid",
                table: "account_address",
                column: "accountid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_address");

            migrationBuilder.DropTable(
                name: "store_location");
        }
    }
}
