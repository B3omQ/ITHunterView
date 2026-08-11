using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCvAnalysisQualityStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "analysis_coverage_json",
                table: "cvs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "analysis_diagnostics_json",
                table: "cvs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "analysis_quality",
                table: "cvs",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cv_analysis_coverage_json",
                table: "cv_job_match_scores",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cv_analysis_diagnostics_json",
                table: "cv_job_match_scores",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cv_analysis_quality",
                table: "cv_job_match_scores",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_cvs_analysis_quality",
                table: "cvs",
                sql: "\"analysis_quality\" IS NULL OR \"analysis_quality\" IN ('COMPLETE', 'PARTIAL', 'INVALID')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cv_job_match_scores_cv_analysis_quality",
                table: "cv_job_match_scores",
                sql: "\"cv_analysis_quality\" IS NULL OR \"cv_analysis_quality\" IN ('COMPLETE', 'PARTIAL', 'INVALID')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_cvs_analysis_quality",
                table: "cvs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cv_job_match_scores_cv_analysis_quality",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "analysis_coverage_json",
                table: "cvs");

            migrationBuilder.DropColumn(
                name: "analysis_diagnostics_json",
                table: "cvs");

            migrationBuilder.DropColumn(
                name: "analysis_quality",
                table: "cvs");

            migrationBuilder.DropColumn(
                name: "cv_analysis_coverage_json",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "cv_analysis_diagnostics_json",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "cv_analysis_quality",
                table: "cv_job_match_scores");
        }
    }
}
