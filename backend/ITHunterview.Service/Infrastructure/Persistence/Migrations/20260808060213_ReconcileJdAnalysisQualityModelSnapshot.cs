using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReconcileJdAnalysisQualityModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $reconcile$
                BEGIN
                    IF (
                        SELECT COUNT(*)
                        FROM information_schema.columns
                        WHERE table_schema = current_schema()
                          AND (
                              (table_name = 'job_analysis_runs'
                               AND column_name IN ('analysis_coverage_json', 'analysis_diagnostics_json', 'analysis_quality'))
                              OR
                              (table_name = 'cv_job_match_scores'
                               AND column_name IN ('jd_analysis_coverage_json', 'jd_analysis_diagnostics_json', 'jd_analysis_quality'))
                          )
                    ) <> 6 THEN
                        RAISE EXCEPTION 'JD_ANALYSIS_QUALITY_SCHEMA_RECONCILIATION_FAILED: expected analysis quality columns are missing';
                    END IF;

                    IF (
                        SELECT COUNT(*)
                        FROM pg_catalog.pg_constraint constraint_row
                        INNER JOIN pg_catalog.pg_class relation_row ON relation_row.oid = constraint_row.conrelid
                        INNER JOIN pg_catalog.pg_namespace namespace_row ON namespace_row.oid = relation_row.relnamespace
                        WHERE namespace_row.nspname = current_schema()
                          AND (
                              (relation_row.relname = 'job_analysis_runs'
                               AND constraint_row.conname = 'ck_job_analysis_runs_analysis_quality')
                              OR
                              (relation_row.relname = 'cv_job_match_scores'
                               AND constraint_row.conname = 'ck_cv_job_match_scores_jd_analysis_quality')
                          )
                    ) <> 2 THEN
                        RAISE EXCEPTION 'JD_ANALYSIS_QUALITY_SCHEMA_RECONCILIATION_FAILED: expected analysis quality constraints are missing';
                    END IF;
                END
                $reconcile$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
