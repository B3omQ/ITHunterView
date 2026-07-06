using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyProfileFieldsAndPendingType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "company_email",
                table: "companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "company_images",
                table: "companies",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact_phone",
                table: "companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "employee_benefits",
                table: "companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "main_field",
                table: "companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "operating_markets",
                table: "companies",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_company_type",
                table: "companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "target_customers",
                table: "companies",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trade_name",
                table: "companies",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "company_email",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "company_images",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "contact_phone",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "employee_benefits",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "main_field",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "operating_markets",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "pending_company_type",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "target_customers",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "trade_name",
                table: "companies");
        }
    }
}
