using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMatchHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("TRUNCATE TABLE cv_job_match_scores;");

            migrationBuilder.AlterColumn<string>(
                name: "raw_jd_text",
                table: "cv_job_match_scores",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<Guid>(
                name: "cv_id",
                table: "cv_job_match_scores",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "cv_file_name",
                table: "cv_job_match_scores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "jd_title",
                table: "cv_job_match_scores",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cv_file_name",
                table: "cv_job_match_scores");

            migrationBuilder.DropColumn(
                name: "jd_title",
                table: "cv_job_match_scores");

            migrationBuilder.AlterColumn<string>(
                name: "raw_jd_text",
                table: "cv_job_match_scores",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "cv_id",
                table: "cv_job_match_scores",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
