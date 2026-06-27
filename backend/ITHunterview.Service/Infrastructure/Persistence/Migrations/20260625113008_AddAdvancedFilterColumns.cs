using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedFilterColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "job_domain",
                table: "job_postings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "level",
                table: "job_postings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "working_model",
                table: "job_postings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "company_type",
                table: "companies",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "job_domain",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "level",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "working_model",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "company_type",
                table: "companies");
        }
    }
}
