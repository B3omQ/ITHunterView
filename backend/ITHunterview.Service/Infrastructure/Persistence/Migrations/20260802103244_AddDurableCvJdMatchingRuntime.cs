using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableCvJdMatchingRuntime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "attempt_count",
                table: "cv_job_match_scores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "billing_reservation_id",
                table: "cv_job_match_scores",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "completed_at",
                table: "cv_job_match_scores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "cv_job_match_scores",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.Sql(
                "UPDATE cv_job_match_scores SET created_at = updated_at;");

            migrationBuilder.AddColumn<string>(
                name: "error_code",
                table: "cv_job_match_scores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "cv_job_match_scores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "idempotency_request_hash",
                table: "cv_job_match_scores",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "input_hash",
                table: "cv_job_match_scores",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "input_snapshot_json",
                table: "cv_job_match_scores",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_heartbeat_at",
                table: "cv_job_match_scores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "lease_expires_at",
                table: "cv_job_match_scores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lease_owner",
                table: "cv_job_match_scores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "lease_token",
                table: "cv_job_match_scores",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "manual_retry_used",
                table: "cv_job_match_scores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "max_attempts",
                table: "cv_job_match_scores",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_attempt_at",
                table: "cv_job_match_scores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "retry_of_job_id",
                table: "cv_job_match_scores",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "started_at",
                table: "cv_job_match_scores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "feature_usage_reservations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    coin_amount = table.Column<int>(type: "integer", nullable: false),
                    deduct_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    refund_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    captured_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    refunded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feature_usage_reservations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cv_job_match_scores_billing_reservation_id",
                table: "cv_job_match_scores",
                column: "billing_reservation_id");

            migrationBuilder.CreateIndex(
                name: "IX_cv_job_match_scores_retry_of_job_id",
                table: "cv_job_match_scores",
                column: "retry_of_job_id",
                unique: true,
                filter: "\"retry_of_job_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_cv_job_match_scores_status_lease_expires_at",
                table: "cv_job_match_scores",
                columns: new[] { "status", "lease_expires_at" },
                filter: "\"match_type\" = 'AI' AND \"status\" = 'Processing'");

            migrationBuilder.CreateIndex(
                name: "IX_cv_job_match_scores_status_next_attempt_at_created_at",
                table: "cv_job_match_scores",
                columns: new[] { "status", "next_attempt_at", "created_at" },
                filter: "\"match_type\" = 'AI' AND \"status\" IN ('Pending', 'RetryScheduled')");

            migrationBuilder.CreateIndex(
                name: "IX_cv_job_match_scores_user_id_idempotency_key",
                table: "cv_job_match_scores",
                columns: new[] { "user_id", "idempotency_key" },
                unique: true,
                filter: "\"match_type\" = 'AI' AND \"idempotency_key\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_credit_transactions_transaction_type_reference_id",
                table: "credit_transactions",
                columns: new[] { "transaction_type", "reference_id" },
                unique: true,
                filter: "\"transaction_type\" = 2 AND \"reference_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_feature_usage_reservations_reference_id",
                table: "feature_usage_reservations",
                column: "reference_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_feature_usage_reservations_user_id_status_feature_key",
                table: "feature_usage_reservations",
                columns: new[] { "user_id", "status", "feature_key" });

            migrationBuilder.AddForeignKey(
                name: "FK_cv_job_match_scores_cv_job_match_scores_retry_of_job_id",
                table: "cv_job_match_scores",
                column: "retry_of_job_id",
                principalTable: "cv_job_match_scores",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_cv_job_match_scores_feature_usage_reservations_billing_rese~",
                table: "cv_job_match_scores",
                column: "billing_reservation_id",
                principalTable: "feature_usage_reservations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cv_job_match_scores_cv_job_match_scores_retry_of_job_id",
                table: "cv_job_match_scores");

            migrationBuilder.DropForeignKey(
                name: "FK_cv_job_match_scores_feature_usage_reservations_billing_rese~",
                table: "cv_job_match_scores");

            migrationBuilder.DropTable(
                name: "feature_usage_reservations");

            migrationBuilder.DropIndex(
                name: "IX_cv_job_match_scores_billing_reservation_id",
                table: "cv_job_match_scores");

            migrationBuilder.DropIndex(
                name: "IX_cv_job_match_scores_retry_of_job_id",
                table: "cv_job_match_scores");

            migrationBuilder.DropIndex(
                name: "IX_cv_job_match_scores_status_lease_expires_at",
                table: "cv_job_match_scores");

            migrationBuilder.DropIndex(
                name: "IX_cv_job_match_scores_status_next_attempt_at_created_at",
                table: "cv_job_match_scores");

            migrationBuilder.DropIndex(
                name: "IX_cv_job_match_scores_user_id_idempotency_key",
                table: "cv_job_match_scores");

            migrationBuilder.DropIndex(
                name: "IX_credit_transactions_transaction_type_reference_id",
                table: "credit_transactions");

            migrationBuilder.DropColumn(
                name: "attempt_count",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "billing_reservation_id",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "error_code",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "idempotency_request_hash",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "input_hash",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "input_snapshot_json",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "last_heartbeat_at",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "lease_expires_at",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "lease_owner",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "lease_token",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "manual_retry_used",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "max_attempts",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "next_attempt_at",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "retry_of_job_id",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "started_at",
                table: "cv_job_match_scores");
        }
    }
}
