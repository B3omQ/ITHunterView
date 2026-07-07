using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "skill_aliases",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    skill_id = table.Column<int>(type: "integer", nullable: false),
                    alias_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    normalized_alias_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_aliases", x => x.id);
                    table.ForeignKey(
                        name: "FK_skill_aliases_skills_skill_id",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_skills_category_id",
                table: "skills",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_skill_aliases_normalized_alias_name",
                table: "skill_aliases",
                column: "normalized_alias_name");

            migrationBuilder.CreateIndex(
                name: "IX_skill_aliases_skill_id",
                table: "skill_aliases",
                column: "skill_id");

            migrationBuilder.AddForeignKey(
                name: "FK_skills_skill_categories_category_id",
                table: "skills",
                column: "category_id",
                principalTable: "skill_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_skills_skill_categories_category_id",
                table: "skills");

            migrationBuilder.DropTable(
                name: "skill_aliases");

            migrationBuilder.DropIndex(
                name: "IX_skills_category_id",
                table: "skills");
        }
    }
}
