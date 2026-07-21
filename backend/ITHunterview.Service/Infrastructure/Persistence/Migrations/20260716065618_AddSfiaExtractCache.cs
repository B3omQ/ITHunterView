using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSfiaExtractCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "sfia_extract_result",
                table: "interview_sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sfia_extract_result",
                table: "cv_job_match_scores",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sfia_extract_result",
                table: "interview_sessions");

            migrationBuilder.DropColumn(
                name: "sfia_extract_result",
                table: "cv_job_match_scores");
        }
    }
}
