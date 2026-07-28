using System;
using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Corrects databases where UpdateJobPostingsFields had already been applied
    /// before its data backfill and non-null constraints were strengthened.
    /// </summary>
    [DbContext(typeof(ITHunterviewContext))]
    [Migration("20260726103000_BackfillJobPostingTextFields")]
    public partial class BackfillJobPostingTextFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE job_postings
                SET
                    income_text = COALESCE(
                        NULLIF(income_text, ''),
                        CASE
                            WHEN min_salary IS NOT NULL AND max_salary IS NOT NULL THEN CONCAT('From ', min_salary::text, ' to ', max_salary::text, ' ', currency)
                            WHEN min_salary IS NOT NULL THEN CONCAT('From ', min_salary::text, ' ', currency)
                            WHEN max_salary IS NOT NULL THEN CONCAT('Up to ', max_salary::text, ' ', currency)
                            ELSE 'Negotiable'
                        END),
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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "income_text",
                table: "job_postings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "work_location_text",
                table: "job_postings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
