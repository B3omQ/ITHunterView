using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJdAnalysisQualityStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "analysis_coverage_json",
                table: "job_analysis_runs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "analysis_diagnostics_json",
                table: "job_analysis_runs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "analysis_quality",
                table: "job_analysis_runs",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "jd_analysis_coverage_json",
                table: "cv_job_match_scores",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "jd_analysis_diagnostics_json",
                table: "cv_job_match_scores",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "jd_analysis_quality",
                table: "cv_job_match_scores",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            // Historical READY runs already passed the strict pre-quality
            // validator. Backfill only that safe subset; failed/processing
            // runs and historical match rows intentionally remain null.
            migrationBuilder.Sql(
                "UPDATE job_analysis_runs SET analysis_quality = 'COMPLETE' WHERE status = 'READY' AND effective_analysis_json IS NOT NULL AND analysis_quality IS NULL;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_job_analysis_runs_analysis_quality",
                table: "job_analysis_runs",
                sql: "\"analysis_quality\" IS NULL OR \"analysis_quality\" IN ('COMPLETE', 'PARTIAL', 'INVALID')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_cv_job_match_scores_jd_analysis_quality",
                table: "cv_job_match_scores",
                sql: "\"jd_analysis_quality\" IS NULL OR \"jd_analysis_quality\" IN ('COMPLETE', 'PARTIAL', 'INVALID')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_job_analysis_runs_analysis_quality",
                table: "job_analysis_runs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cv_job_match_scores_jd_analysis_quality",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "analysis_coverage_json",
                table: "job_analysis_runs");

            migrationBuilder.DropColumn(
                name: "analysis_diagnostics_json",
                table: "job_analysis_runs");

            migrationBuilder.DropColumn(
                name: "analysis_quality",
                table: "job_analysis_runs");

            migrationBuilder.DropColumn(
                name: "jd_analysis_coverage_json",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "jd_analysis_diagnostics_json",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "jd_analysis_quality",
                table: "cv_job_match_scores");
        }
    }
}
