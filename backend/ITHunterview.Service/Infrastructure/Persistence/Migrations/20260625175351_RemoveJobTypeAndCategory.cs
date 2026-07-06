using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveJobTypeAndCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category_id",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "job_type",
                table: "job_postings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "category_id",
                table: "job_postings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "job_type",
                table: "job_postings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
