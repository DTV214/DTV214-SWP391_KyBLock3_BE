using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TetGift.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ForceNormalizeRoleValuesToUppercase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Force normalize tất cả role values sang UPPERCASE
            // Update tất cả các giá trị không phải UPPERCASE
            migrationBuilder.Sql(@"
                UPDATE account 
                SET role = UPPER(role) 
                WHERE role IS NOT NULL AND role != UPPER(role);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
