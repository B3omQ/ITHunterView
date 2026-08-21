using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchingProductBoundaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "failure_code",
                table: "recruiter_unlocked_cvs",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "snapshot_content_hash",
                table: "recruiter_unlocked_cvs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "snapshot_created_at",
                table: "recruiter_unlocked_cvs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "snapshot_file_name",
                table: "recruiter_unlocked_cvs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "snapshot_storage_key",
                table: "recruiter_unlocked_cvs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_scan_result_id",
                table: "recruiter_unlocked_cvs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "recruiter_unlocked_cvs",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "COMPLETED");

            migrationBuilder.AddColumn<string>(
                name: "product_scope",
                table: "cv_job_match_scores",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "candidate_job_scan_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cv_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cv_file_name_snapshot = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_job_scan_runs", x => x.id);
                    table.CheckConstraint("ck_candidate_job_scan_runs_status", "\"status\" IN ('PENDING', 'PROCESSING', 'COMPLETED', 'FAILED')");
                    table.ForeignKey(
                        name: "FK_candidate_job_scan_runs_cvs_cv_id",
                        column: x => x.cv_id,
                        principalTable: "cvs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_candidate_job_scan_runs_users_candidate_user_id",
                        column: x => x.candidate_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recruiter_cv_scan_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recruiter_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recruiter_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_title_snapshot = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recruiter_cv_scan_runs", x => x.id);
                    table.CheckConstraint("ck_recruiter_cv_scan_runs_status", "\"status\" IN ('PENDING', 'PROCESSING', 'COMPLETED', 'FAILED')");
                    table.ForeignKey(
                        name: "FK_recruiter_cv_scan_runs_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recruiter_cv_scan_runs_job_postings_job_id",
                        column: x => x.job_id,
                        principalTable: "job_postings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recruiter_cv_scan_runs_recruiter_profiles_recruiter_profile~",
                        column: x => x.recruiter_profile_id,
                        principalTable: "recruiter_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recruiter_cv_scan_runs_users_recruiter_user_id",
                        column: x => x.recruiter_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "candidate_job_scan_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_title_snapshot = table.Column<string>(type: "text", nullable: false),
                    match_score = table.Column<decimal>(type: "numeric", nullable: true),
                    match_details = table.Column<string>(type: "text", nullable: false),
                    cv_analysis_quality = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    cv_analysis_coverage_json = table.Column<string>(type: "jsonb", nullable: true),
                    cv_analysis_diagnostics_json = table.Column<string>(type: "jsonb", nullable: true),
                    rank = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_job_scan_results", x => x.id);
                    table.CheckConstraint("ck_candidate_job_scan_results_cv_analysis_quality", "\"cv_analysis_quality\" IS NULL OR \"cv_analysis_quality\" IN ('COMPLETE', 'PARTIAL', 'INVALID')");
                    table.ForeignKey(
                        name: "FK_candidate_job_scan_results_candidate_job_scan_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "candidate_job_scan_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_candidate_job_scan_results_job_postings_job_id",
                        column: x => x.job_id,
                        principalTable: "job_postings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recruiter_cv_scan_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cv_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_score = table.Column<decimal>(type: "numeric", nullable: true),
                    match_details = table.Column<string>(type: "text", nullable: false),
                    cv_analysis_quality = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    cv_analysis_coverage_json = table.Column<string>(type: "jsonb", nullable: true),
                    cv_analysis_diagnostics_json = table.Column<string>(type: "jsonb", nullable: true),
                    rank = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recruiter_cv_scan_results", x => x.id);
                    table.CheckConstraint("ck_recruiter_cv_scan_results_cv_analysis_quality", "\"cv_analysis_quality\" IS NULL OR \"cv_analysis_quality\" IN ('COMPLETE', 'PARTIAL', 'INVALID')");
                    table.ForeignKey(
                        name: "FK_recruiter_cv_scan_results_cvs_cv_id",
                        column: x => x.cv_id,
                        principalTable: "cvs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recruiter_cv_scan_results_recruiter_cv_scan_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "recruiter_cv_scan_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_recruiter_cv_scan_results_users_candidate_user_id",
                        column: x => x.candidate_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recruiter_unlocked_cvs_cv_id",
                table: "recruiter_unlocked_cvs",
                column: "cv_id");

            migrationBuilder.CreateIndex(
                name: "IX_recruiter_unlocked_cvs_source_scan_result_id",
                table: "recruiter_unlocked_cvs",
                column: "source_scan_result_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_recruiter_unlocked_cvs_status",
                table: "recruiter_unlocked_cvs",
                sql: "\"status\" IN ('PENDING', 'COMPLETED', 'FAILED')");

            migrationBuilder.CreateIndex(
                name: "IX_cv_job_match_scores_product_scope_user_id_updated_at",
                table: "cv_job_match_scores",
                columns: new[] { "product_scope", "user_id", "updated_at" },
                descending: new[] { false, false, true });

            migrationBuilder.AddCheckConstraint(
                name: "ck_cv_job_match_scores_product_scope",
                table: "cv_job_match_scores",
                sql: "\"product_scope\" IS NULL OR \"product_scope\" IN ('CANDIDATE_ONE_TO_ONE')");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_job_scan_results_job_id",
                table: "candidate_job_scan_results",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_job_scan_results_run_id_job_id",
                table: "candidate_job_scan_results",
                columns: new[] { "run_id", "job_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_candidate_job_scan_runs_candidate_user_id_cv_id_status_crea~",
                table: "candidate_job_scan_runs",
                columns: new[] { "candidate_user_id", "cv_id", "status", "created_at" },
                descending: new[] { false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_candidate_job_scan_runs_cv_id",
                table: "candidate_job_scan_runs",
                column: "cv_id");

            migrationBuilder.CreateIndex(
                name: "IX_recruiter_cv_scan_results_candidate_user_id",
                table: "recruiter_cv_scan_results",
                column: "candidate_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_recruiter_cv_scan_results_cv_id",
                table: "recruiter_cv_scan_results",
                column: "cv_id");

            migrationBuilder.CreateIndex(
                name: "IX_recruiter_cv_scan_results_run_id_cv_id",
                table: "recruiter_cv_scan_results",
                columns: new[] { "run_id", "cv_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recruiter_cv_scan_runs_company_id",
                table: "recruiter_cv_scan_runs",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_recruiter_cv_scan_runs_job_id",
                table: "recruiter_cv_scan_runs",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "IX_recruiter_cv_scan_runs_recruiter_profile_id",
                table: "recruiter_cv_scan_runs",
                column: "recruiter_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_recruiter_cv_scan_runs_recruiter_user_id_company_id_job_id_~",
                table: "recruiter_cv_scan_runs",
                columns: new[] { "recruiter_user_id", "company_id", "job_id", "status", "created_at" },
                descending: new[] { false, false, false, false, true });

            migrationBuilder.AddForeignKey(
                name: "FK_recruiter_unlocked_cvs_cvs_cv_id",
                table: "recruiter_unlocked_cvs",
                column: "cv_id",
                principalTable: "cvs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_recruiter_unlocked_cvs_recruiter_cv_scan_results_source_sca~",
                table: "recruiter_unlocked_cvs",
                column: "source_scan_result_id",
                principalTable: "recruiter_cv_scan_results",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_recruiter_unlocked_cvs_users_recruiter_id",
                table: "recruiter_unlocked_cvs",
                column: "recruiter_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_recruiter_unlocked_cvs_cvs_cv_id",
                table: "recruiter_unlocked_cvs");

            migrationBuilder.DropForeignKey(
                name: "FK_recruiter_unlocked_cvs_recruiter_cv_scan_results_source_sca~",
                table: "recruiter_unlocked_cvs");

            migrationBuilder.DropForeignKey(
                name: "FK_recruiter_unlocked_cvs_users_recruiter_id",
                table: "recruiter_unlocked_cvs");

            migrationBuilder.DropTable(
                name: "candidate_job_scan_results");

            migrationBuilder.DropTable(
                name: "recruiter_cv_scan_results");

            migrationBuilder.DropTable(
                name: "candidate_job_scan_runs");

            migrationBuilder.DropTable(
                name: "recruiter_cv_scan_runs");

            migrationBuilder.DropIndex(
                name: "IX_recruiter_unlocked_cvs_cv_id",
                table: "recruiter_unlocked_cvs");

            migrationBuilder.DropIndex(
                name: "IX_recruiter_unlocked_cvs_source_scan_result_id",
                table: "recruiter_unlocked_cvs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_recruiter_unlocked_cvs_status",
                table: "recruiter_unlocked_cvs");

            migrationBuilder.DropIndex(
                name: "IX_cv_job_match_scores_product_scope_user_id_updated_at",
                table: "cv_job_match_scores");

            migrationBuilder.DropCheckConstraint(
                name: "ck_cv_job_match_scores_product_scope",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "failure_code",
                table: "recruiter_unlocked_cvs");

            migrationBuilder.DropColumn(
                name: "snapshot_content_hash",
                table: "recruiter_unlocked_cvs");

            migrationBuilder.DropColumn(
                name: "snapshot_created_at",
                table: "recruiter_unlocked_cvs");

            migrationBuilder.DropColumn(
                name: "snapshot_file_name",
                table: "recruiter_unlocked_cvs");

            migrationBuilder.DropColumn(
                name: "snapshot_storage_key",
                table: "recruiter_unlocked_cvs");

            migrationBuilder.DropColumn(
                name: "source_scan_result_id",
                table: "recruiter_unlocked_cvs");

            migrationBuilder.DropColumn(
                name: "status",
                table: "recruiter_unlocked_cvs");

            migrationBuilder.DropColumn(
                name: "product_scope",
                table: "cv_job_match_scores");
        }
    }
}
