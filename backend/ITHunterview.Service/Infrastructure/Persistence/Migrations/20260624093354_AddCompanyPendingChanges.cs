using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "has_pending_change",
                table: "companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "pending_headquarters_address",
                table: "companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_name",
                table: "companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_tax_code",
                table: "companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_verification_document_url",
                table: "companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pending_verification_method",
                table: "companies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reject_reason",
                table: "companies",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "has_pending_change",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "pending_headquarters_address",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "pending_name",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "pending_tax_code",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "pending_verification_document_url",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "pending_verification_method",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "reject_reason",
                table: "companies");
        }
    }
}
