using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDifficultyAndSampleAnswer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "difficulty",
                table: "interview_question_bank");

            migrationBuilder.DropColumn(
                name: "sample_answer",
                table: "interview_question_bank");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "difficulty",
                table: "interview_question_bank",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sample_answer",
                table: "interview_question_bank",
                type: "text",
                nullable: true);
        }
    }
}
