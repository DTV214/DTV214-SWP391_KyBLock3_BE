using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TetGift.DAL.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeRoleValuesToUppercase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Normalize Role values: PascalCase -> UPPERCASE
            migrationBuilder.Sql(@"
                UPDATE account 
                SET role = 'CUSTOMER' 
                WHERE LOWER(role) = 'customer' AND role != 'CUSTOMER';
            ");

            migrationBuilder.Sql(@"
                UPDATE account 
                SET role = 'ADMIN' 
                WHERE LOWER(role) = 'admin' AND role != 'ADMIN';
            ");

            migrationBuilder.Sql(@"
                UPDATE account 
                SET role = 'STAFF' 
                WHERE LOWER(role) = 'staff' AND role != 'STAFF';
            ");

            // Normalize tất cả role values sang UPPERCASE (fallback)
            migrationBuilder.Sql(@"
                UPDATE account 
                SET role = UPPER(role) 
                WHERE role IS NOT NULL AND role != UPPER(role);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không cần rollback vì đây là data migration, không thay đổi schema
            // Nếu cần rollback, có thể convert ngược lại nhưng không khuyến khích
        }
    }
}
