using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParseStatusAndErrorToCvsAndJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "parse_error",
                table: "job_postings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "parse_status",
                table: "job_postings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "parse_error",
                table: "cvs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "parse_status",
                table: "cvs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "parse_error",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "parse_status",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "parse_error",
                table: "cvs");

            migrationBuilder.DropColumn(
                name: "parse_status",
                table: "cvs");
        }
    }
}
