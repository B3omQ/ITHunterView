using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateJobPostingsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This index change is part of the current EF model snapshot.
            migrationBuilder.DropIndex(
                name: "IX_cvs_user_id",
                table: "cvs");

            migrationBuilder.RenameColumn(
                name: "detailed_location",
                table: "job_postings",
                newName: "work_location_text");

            migrationBuilder.AddColumn<string>(
                name: "income_text",
                table: "job_postings",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE job_postings
                SET description = CONCAT(description, E'\n\nResponsibilities:\n', responsibilities)
                WHERE responsibilities IS NOT NULL
                  AND responsibilities <> ''
                  AND POSITION('Responsibilities:' IN description) = 0;

                UPDATE job_postings
                SET
                    income_text = CASE
                        WHEN min_salary IS NOT NULL AND max_salary IS NOT NULL THEN CONCAT('From ', min_salary::text, ' to ', max_salary::text, ' ', currency)
                        WHEN min_salary IS NOT NULL THEN CONCAT('From ', min_salary::text, ' ', currency)
                        WHEN max_salary IS NOT NULL THEN CONCAT('Up to ', max_salary::text, ' ', currency)
                        ELSE 'Negotiable'
                    END,
                    work_location_text = COALESCE(NULLIF(work_location_text, ''), location, ''),
                    parsed_data = NULL,
                    parse_status = 'PENDING',
                    parse_error = NULL,
                    skills_embedding = NULL,
                    experience_embedding = NULL,
                    domain_embedding = NULL;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "income_text",
                table: "job_postings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "work_location_text",
                table: "job_postings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "responsibilities",
                table: "job_postings");

            migrationBuilder.CreateIndex(
                name: "IX_cvs_user_id_is_primary",
                table: "cvs",
                columns: new[] { "user_id", "is_primary" },
                unique: true,
                filter: "\"is_primary\" = true AND \"deleted_at\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cvs_user_id_is_primary",
                table: "cvs");

            migrationBuilder.DropColumn(
                name: "income_text",
                table: "job_postings");

            migrationBuilder.AlterColumn<string>(
                name: "work_location_text",
                table: "job_postings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.RenameColumn(
                name: "work_location_text",
                table: "job_postings",
                newName: "detailed_location");

            migrationBuilder.AddColumn<string>(
                name: "responsibilities",
                table: "job_postings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_cvs_user_id",
                table: "cvs",
                column: "user_id");
        }
    }
}
