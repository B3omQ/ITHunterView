using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserActivityLogsIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_user_activity_logs_created_at",
                table: "user_activity_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_user_activity_logs_operation_type",
                table: "user_activity_logs",
                column: "operation_type");

            migrationBuilder.CreateIndex(
                name: "IX_user_activity_logs_table_name",
                table: "user_activity_logs",
                column: "table_name");

            migrationBuilder.CreateIndex(
                name: "IX_user_activity_logs_user_id",
                table: "user_activity_logs",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_activity_logs_created_at",
                table: "user_activity_logs");

            migrationBuilder.DropIndex(
                name: "IX_user_activity_logs_operation_type",
                table: "user_activity_logs");

            migrationBuilder.DropIndex(
                name: "IX_user_activity_logs_table_name",
                table: "user_activity_logs");

            migrationBuilder.DropIndex(
                name: "IX_user_activity_logs_user_id",
                table: "user_activity_logs");
        }
    }
}
