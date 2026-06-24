using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class fix01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "candidate_experiences",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "candidate_certifications",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_user_skills_skill_id",
                table: "user_skills",
                column: "skill_id");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_experiences_company_id",
                table: "candidate_experiences",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_experiences_user_id",
                table: "candidate_experiences",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_educations_major_id",
                table: "candidate_educations",
                column: "major_id");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_educations_user_id",
                table: "candidate_educations",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_certifications_user_id",
                table: "candidate_certifications",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_candidate_certifications_users_user_id",
                table: "candidate_certifications",
                column: "user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_candidate_educations_majors_major_id",
                table: "candidate_educations",
                column: "major_id",
                principalTable: "majors",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_candidate_educations_users_user_id",
                table: "candidate_educations",
                column: "user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_candidate_experiences_companies_company_id",
                table: "candidate_experiences",
                column: "company_id",
                principalTable: "companies",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_candidate_experiences_users_user_id",
                table: "candidate_experiences",
                column: "user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_skills_skills_skill_id",
                table: "user_skills",
                column: "skill_id",
                principalTable: "skills",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_candidate_certifications_users_user_id",
                table: "candidate_certifications");

            migrationBuilder.DropForeignKey(
                name: "FK_candidate_educations_majors_major_id",
                table: "candidate_educations");

            migrationBuilder.DropForeignKey(
                name: "FK_candidate_educations_users_user_id",
                table: "candidate_educations");

            migrationBuilder.DropForeignKey(
                name: "FK_candidate_experiences_companies_company_id",
                table: "candidate_experiences");

            migrationBuilder.DropForeignKey(
                name: "FK_candidate_experiences_users_user_id",
                table: "candidate_experiences");

            migrationBuilder.DropForeignKey(
                name: "FK_user_skills_skills_skill_id",
                table: "user_skills");

            migrationBuilder.DropIndex(
                name: "IX_user_skills_skill_id",
                table: "user_skills");

            migrationBuilder.DropIndex(
                name: "IX_candidate_experiences_company_id",
                table: "candidate_experiences");

            migrationBuilder.DropIndex(
                name: "IX_candidate_experiences_user_id",
                table: "candidate_experiences");

            migrationBuilder.DropIndex(
                name: "IX_candidate_educations_major_id",
                table: "candidate_educations");

            migrationBuilder.DropIndex(
                name: "IX_candidate_educations_user_id",
                table: "candidate_educations");

            migrationBuilder.DropIndex(
                name: "IX_candidate_certifications_user_id",
                table: "candidate_certifications");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "candidate_experiences",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "candidate_certifications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);
        }
    }
}
