using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCvOptimizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cv_optimizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cv_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_jd_text = table.Column<string>(type: "text", nullable: true),
                    feedback_data = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cv_optimizations", x => x.id);
                    table.ForeignKey(
                        name: "FK_cv_optimizations_cvs_cv_id",
                        column: x => x.cv_id,
                        principalTable: "cvs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cv_optimizations_users_candidate_id",
                        column: x => x.candidate_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cv_optimizations_candidate_id",
                table: "cv_optimizations",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "IX_cv_optimizations_cv_id",
                table: "cv_optimizations",
                column: "cv_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cv_optimizations");
        }
    }
}
