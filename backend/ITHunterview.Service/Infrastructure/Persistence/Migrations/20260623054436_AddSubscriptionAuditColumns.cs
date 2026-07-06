using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionAuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "subscriptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            // Thay đổi kiểu cột features_config sang jsonb sử dụng raw SQL để cast dữ liệu cũ
            migrationBuilder.Sql("ALTER TABLE subscriptions ALTER COLUMN features_config TYPE jsonb USING features_config::jsonb;");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "subscriptions");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "subscriptions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            // Cast ngược lại sang text
            migrationBuilder.Sql("ALTER TABLE subscriptions ALTER COLUMN features_config TYPE text USING features_config::text;");
        }
    }
}
