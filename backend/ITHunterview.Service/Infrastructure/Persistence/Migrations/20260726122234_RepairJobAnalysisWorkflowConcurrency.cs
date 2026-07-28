using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RepairJobAnalysisWorkflowConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_job_analysis_runs_job_id_input_hash",
                table: "job_analysis_runs");

            migrationBuilder.DropIndex(
                name: "IX_job_analysis_runs_job_id_input_revision",
                table: "job_analysis_runs");

            migrationBuilder.AddColumn<int>(
                name: "effective_analysis_revision",
                table: "job_postings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "effective_analysis_run_id",
                table: "job_postings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "semantic_content_hash",
                table: "job_postings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "attempt_number",
                table: "job_analysis_runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "decision_version",
                table: "job_analysis_runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "job_analysis_runs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_heartbeat_at",
                table: "job_analysis_runs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "lease_expires_at",
                table: "job_analysis_runs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "provider_call_started_at",
                table: "job_analysis_runs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_postings_effective_analysis_run_id",
                table: "job_postings",
                column: "effective_analysis_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_analysis_runs_job_id_idempotency_key",
                table: "job_analysis_runs",
                columns: new[] { "job_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_job_analysis_runs_job_id_input_revision",
                table: "job_analysis_runs",
                columns: new[] { "job_id", "input_revision" },
                unique: true,
                filter: "status IN ('PENDING', 'PROCESSING')");

            migrationBuilder.CreateIndex(
                name: "IX_job_analysis_runs_job_id_input_revision_attempt_number",
                table: "job_analysis_runs",
                columns: new[] { "job_id", "input_revision", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_analysis_runs_job_id_input_revision_input_hash",
                table: "job_analysis_runs",
                columns: new[] { "job_id", "input_revision", "input_hash" });

            migrationBuilder.AddForeignKey(
                name: "FK_job_postings_job_analysis_runs_effective_analysis_run_id",
                table: "job_postings",
                column: "effective_analysis_run_id",
                principalTable: "job_analysis_runs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_job_postings_job_analysis_runs_effective_analysis_run_id",
                table: "job_postings");

            migrationBuilder.DropIndex(
                name: "IX_job_postings_effective_analysis_run_id",
                table: "job_postings");

            migrationBuilder.DropIndex(
                name: "IX_job_analysis_runs_job_id_idempotency_key",
                table: "job_analysis_runs");

            migrationBuilder.DropIndex(
                name: "IX_job_analysis_runs_job_id_input_revision",
                table: "job_analysis_runs");

            migrationBuilder.DropIndex(
                name: "IX_job_analysis_runs_job_id_input_revision_attempt_number",
                table: "job_analysis_runs");

            migrationBuilder.DropIndex(
                name: "IX_job_analysis_runs_job_id_input_revision_input_hash",
                table: "job_analysis_runs");

            migrationBuilder.DropColumn(
                name: "effective_analysis_revision",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "effective_analysis_run_id",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "semantic_content_hash",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "attempt_number",
                table: "job_analysis_runs");

            migrationBuilder.DropColumn(
                name: "decision_version",
                table: "job_analysis_runs");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "job_analysis_runs");

            migrationBuilder.DropColumn(
                name: "last_heartbeat_at",
                table: "job_analysis_runs");

            migrationBuilder.DropColumn(
                name: "lease_expires_at",
                table: "job_analysis_runs");

            migrationBuilder.DropColumn(
                name: "provider_call_started_at",
                table: "job_analysis_runs");

            migrationBuilder.CreateIndex(
                name: "IX_job_analysis_runs_job_id_input_hash",
                table: "job_analysis_runs",
                columns: new[] { "job_id", "input_hash" });

            migrationBuilder.CreateIndex(
                name: "IX_job_analysis_runs_job_id_input_revision",
                table: "job_analysis_runs",
                columns: new[] { "job_id", "input_revision" },
                unique: true);
        }
    }
}
