using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruiterUnlockedCvsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "recruiter_unlocked_cvs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recruiter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cv_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    coins_spent = table.Column<int>(type: "integer", nullable: false),
                    unlocked_via = table.Column<string>(type: "text", nullable: false),
                    unlocked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recruiter_unlocked_cvs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recruiter_unlocked_cvs_recruiter_id_cv_id",
                table: "recruiter_unlocked_cvs",
                columns: new[] { "recruiter_id", "cv_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recruiter_unlocked_cvs");
        }
    }
}
