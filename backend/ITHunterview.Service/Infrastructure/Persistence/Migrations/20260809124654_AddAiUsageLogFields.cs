using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiUsageLogFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "feature_code",
                table: "ai_api_usage_logs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "latency_ms",
                table: "ai_api_usage_logs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_name",
                table: "ai_api_usage_logs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "ai_api_usage_logs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "ai_api_usage_logs",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "feature_code",
                table: "ai_api_usage_logs");

            migrationBuilder.DropColumn(
                name: "latency_ms",
                table: "ai_api_usage_logs");

            migrationBuilder.DropColumn(
                name: "provider_name",
                table: "ai_api_usage_logs");

            migrationBuilder.DropColumn(
                name: "status",
                table: "ai_api_usage_logs");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "ai_api_usage_logs");
        }
    }
}
