using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPushedTopUntilToJobPostings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cvs_user_id",
                table: "cvs");

            migrationBuilder.AddColumn<DateTime>(
                name: "pushed_top_until",
                table: "job_postings",
                type: "timestamp with time zone",
                nullable: true);

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
                name: "pushed_top_until",
                table: "job_postings");

            migrationBuilder.CreateIndex(
                name: "IX_cvs_user_id",
                table: "cvs",
                column: "user_id");
        }
    }
}
