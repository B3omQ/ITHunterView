using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSfiaSkillsAndTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM learning_paths;");

            migrationBuilder.CreateTable(
                name: "sfia_skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    skill_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    category = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    subcategory = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sfia_skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "target_role_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_target_role_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "target_role_skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sfia_skill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_level = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_target_role_skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_target_role_skills_sfia_skills_sfia_skill_id",
                        column: x => x.sfia_skill_id,
                        principalTable: "sfia_skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_target_role_skills_target_role_templates_role_template_id",
                        column: x => x.role_template_id,
                        principalTable: "target_role_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_target_role_skills_role_template_id",
                table: "target_role_skills",
                column: "role_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_target_role_skills_sfia_skill_id",
                table: "target_role_skills",
                column: "sfia_skill_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "target_role_skills");

            migrationBuilder.DropTable(
                name: "sfia_skills");

            migrationBuilder.DropTable(
                name: "target_role_templates");
        }
    }
}
