using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToUserWalletsAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {




            // Cleanup duplicate user_wallets (keep the latest one)
            migrationBuilder.Sql(@"
                DELETE FROM user_wallets
                WHERE id IN (
                    SELECT id
                    FROM (
                        SELECT id, ROW_NUMBER() OVER (PARTITION BY user_id ORDER BY updated_at DESC) as rnum
                        FROM user_wallets
                    ) t
                    WHERE t.rnum > 1
                );
            ");

            migrationBuilder.CreateIndex(
                name: "IX_user_wallets_user_id",
                table: "user_wallets",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_order_code",
                table: "payments",
                column: "order_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_wallets_user_id",
                table: "user_wallets");

            migrationBuilder.DropIndex(
                name: "IX_payments_order_code",
                table: "payments");
        }
    }
}
