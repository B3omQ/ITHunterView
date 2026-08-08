using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchHistorySoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "history_hidden_at",
                table: "cv_job_match_scores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_cv_job_match_scores_user_id_history_hidden_at_updated_at",
                table: "cv_job_match_scores",
                columns: new[] { "user_id", "history_hidden_at", "updated_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cv_job_match_scores_user_id_history_hidden_at_updated_at",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "history_hidden_at",
                table: "cv_job_match_scores");
        }
    }
}
