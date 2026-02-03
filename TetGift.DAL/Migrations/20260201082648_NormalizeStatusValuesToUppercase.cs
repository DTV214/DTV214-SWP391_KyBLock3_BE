using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TetGift.DAL.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeStatusValuesToUppercase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Normalize Account Status: PascalCase -> UPPERCASE
            migrationBuilder.Sql(@"
                UPDATE account 
                SET status = 'PENDING' 
                WHERE LOWER(status) = 'pending' AND status != 'PENDING';
            ");

            migrationBuilder.Sql(@"
                UPDATE account 
                SET status = 'ACTIVE' 
                WHERE LOWER(status) = 'active' AND status != 'ACTIVE';
            ");

            // Normalize Order Status: PascalCase -> UPPERCASE
            migrationBuilder.Sql(@"
                UPDATE orders 
                SET status = 'PENDING' 
                WHERE LOWER(status) = 'pending' AND status != 'PENDING';
            ");

            migrationBuilder.Sql(@"
                UPDATE orders 
                SET status = 'CONFIRMED' 
                WHERE LOWER(status) = 'confirmed' AND status != 'CONFIRMED';
            ");

            migrationBuilder.Sql(@"
                UPDATE orders 
                SET status = 'PROCESSING' 
                WHERE LOWER(status) = 'processing' AND status != 'PROCESSING';
            ");

            migrationBuilder.Sql(@"
                UPDATE orders 
                SET status = 'SHIPPED' 
                WHERE LOWER(status) = 'shipped' AND status != 'SHIPPED';
            ");

            migrationBuilder.Sql(@"
                UPDATE orders 
                SET status = 'DELIVERED' 
                WHERE LOWER(status) = 'delivered' AND status != 'DELIVERED';
            ");

            migrationBuilder.Sql(@"
                UPDATE orders 
                SET status = 'CANCELLED' 
                WHERE LOWER(status) = 'cancelled' AND status != 'CANCELLED';
            ");

            // Normalize Product Status: PascalCase -> UPPERCASE
            migrationBuilder.Sql(@"
                UPDATE product 
                SET status = 'ACTIVE' 
                WHERE LOWER(status) = 'active' AND status != 'ACTIVE';
            ");

            migrationBuilder.Sql(@"
                UPDATE product 
                SET status = 'DELETED' 
                WHERE LOWER(status) = 'deleted' AND status != 'DELETED';
            ");

            migrationBuilder.Sql(@"
                UPDATE product 
                SET status = 'INACTIVE' 
                WHERE LOWER(status) = 'inactive' AND status != 'INACTIVE';
            ");

            // Normalize Payment Status (nếu có dữ liệu cũ)
            migrationBuilder.Sql(@"
                UPDATE payment 
                SET status = 'PENDING' 
                WHERE LOWER(status) = 'pending' AND status != 'PENDING';
            ");

            migrationBuilder.Sql(@"
                UPDATE payment 
                SET status = 'SUCCESS' 
                WHERE LOWER(status) = 'success' AND status != 'SUCCESS';
            ");

            migrationBuilder.Sql(@"
                UPDATE payment 
                SET status = 'FAILED' 
                WHERE LOWER(status) = 'failed' AND status != 'FAILED';
            ");

            migrationBuilder.Sql(@"
                UPDATE payment 
                SET status = 'REFUNDED' 
                WHERE LOWER(status) = 'refunded' AND status != 'REFUNDED';
            ");

            // Normalize Stock Status (nếu có dữ liệu cũ)
            migrationBuilder.Sql(@"
                UPDATE stock 
                SET status = 'ACTIVE' 
                WHERE LOWER(status) = 'active' AND status != 'ACTIVE';
            ");

            migrationBuilder.Sql(@"
                UPDATE stock 
                SET status = 'OUT_OF_STOCK' 
                WHERE LOWER(status) = 'out_of_stock' AND status != 'OUT_OF_STOCK';
            ");

            migrationBuilder.Sql(@"
                UPDATE stock 
                SET status = 'DELETED' 
                WHERE LOWER(status) = 'deleted' AND status != 'DELETED';
            ");

            migrationBuilder.Sql(@"
                UPDATE stock 
                SET status = 'EXPIRED' 
                WHERE LOWER(status) = 'expired' AND status != 'EXPIRED';
            ");

            // Normalize Wallet Status (nếu có dữ liệu cũ)
            migrationBuilder.Sql(@"
                UPDATE wallet 
                SET status = 'ACTIVE' 
                WHERE LOWER(status) = 'active' AND status != 'ACTIVE';
            ");

            migrationBuilder.Sql(@"
                UPDATE wallet 
                SET status = 'INACTIVE' 
                WHERE LOWER(status) = 'inactive' AND status != 'INACTIVE';
            ");

            // Normalize WalletTransaction Status (nếu có dữ liệu cũ)
            migrationBuilder.Sql(@"
                UPDATE wallet_transaction 
                SET status = 'SUCCESS' 
                WHERE LOWER(status) = 'success' AND status != 'SUCCESS';
            ");

            migrationBuilder.Sql(@"
                UPDATE wallet_transaction 
                SET status = 'FAILED' 
                WHERE LOWER(status) = 'failed' AND status != 'FAILED';
            ");

            migrationBuilder.Sql(@"
                UPDATE wallet_transaction 
                SET status = 'PENDING' 
                WHERE LOWER(status) = 'pending' AND status != 'PENDING';
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
