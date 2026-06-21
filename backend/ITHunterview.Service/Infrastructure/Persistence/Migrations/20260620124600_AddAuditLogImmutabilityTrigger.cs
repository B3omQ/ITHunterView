using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogImmutabilityTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "operation_type",
                table: "user_activity_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "snapshot_diff",
                table: "user_activity_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "table_name",
                table: "user_activity_logs",
                type: "text",
                nullable: true);

            // Tạo trigger ngăn chặn UPDATE/DELETE trên bảng user_activity_logs
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION prevent_audit_log_mutation()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF TG_OP = 'DELETE' AND current_setting('app.allow_audit_log_delete', true) = 'true' THEN
                        RETURN OLD;
                    END IF;
                    
                    RAISE EXCEPTION 'Bang user_activity_logs la bat bien (Insert-Only). Khong duoc phep sua doi hoac xoa du lieu logs!';
                END;
                $$ LANGUAGE plpgsql;
            ");

            migrationBuilder.Sql(@"
                CREATE TRIGGER trg_prevent_audit_log_mutation
                BEFORE UPDATE OR DELETE ON user_activity_logs
                FOR EACH ROW
                EXECUTE FUNCTION prevent_audit_log_mutation();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_prevent_audit_log_mutation ON user_activity_logs;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS prevent_audit_log_mutation;");

            migrationBuilder.DropColumn(
                name: "operation_type",
                table: "user_activity_logs");

            migrationBuilder.DropColumn(
                name: "snapshot_diff",
                table: "user_activity_logs");

            migrationBuilder.DropColumn(
                name: "table_name",
                table: "user_activity_logs");
        }
    }
}
