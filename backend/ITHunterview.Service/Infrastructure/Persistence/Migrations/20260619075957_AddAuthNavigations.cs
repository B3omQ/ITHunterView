using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthNavigations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "position_title",
                table: "recruiter_profiles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "recruiter_profiles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "full_name",
                table: "recruiter_profiles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "avatar_url",
                table: "recruiter_profiles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "portfolio_url",
                table: "candidate_profiles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "candidate_profiles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "location",
                table: "candidate_profiles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "linkedIn_url",
                table: "candidate_profiles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "last_name",
                table: "candidate_profiles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "github_url",
                table: "candidate_profiles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "first_name",
                table: "candidate_profiles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "avatar_url",
                table: "candidate_profiles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "about_me",
                table: "candidate_profiles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_users_role_id",
                table: "users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_recruiter_profiles_user_id",
                table: "recruiter_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_password_resets_user_id",
                table: "password_resets",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_email_verification_tokens_user_id",
                table: "email_verification_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_profiles_user_id",
                table: "candidate_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_candidate_profiles_users_user_id",
                table: "candidate_profiles",
                column: "user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_email_verification_tokens_users_user_id",
                table: "email_verification_tokens",
                column: "user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_password_resets_users_user_id",
                table: "password_resets",
                column: "user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_recruiter_profiles_users_user_id",
                table: "recruiter_profiles",
                column: "user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_users_roles_role_id",
                table: "users",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_candidate_profiles_users_user_id",
                table: "candidate_profiles");

            migrationBuilder.DropForeignKey(
                name: "FK_email_verification_tokens_users_user_id",
                table: "email_verification_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_password_resets_users_user_id",
                table: "password_resets");

            migrationBuilder.DropForeignKey(
                name: "FK_recruiter_profiles_users_user_id",
                table: "recruiter_profiles");

            migrationBuilder.DropForeignKey(
                name: "FK_users_roles_role_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_role_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_recruiter_profiles_user_id",
                table: "recruiter_profiles");

            migrationBuilder.DropIndex(
                name: "IX_password_resets_user_id",
                table: "password_resets");

            migrationBuilder.DropIndex(
                name: "IX_email_verification_tokens_user_id",
                table: "email_verification_tokens");

            migrationBuilder.DropIndex(
                name: "IX_candidate_profiles_user_id",
                table: "candidate_profiles");

            migrationBuilder.AlterColumn<string>(
                name: "position_title",
                table: "recruiter_profiles",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "recruiter_profiles",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "full_name",
                table: "recruiter_profiles",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "avatar_url",
                table: "recruiter_profiles",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "portfolio_url",
                table: "candidate_profiles",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "candidate_profiles",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "location",
                table: "candidate_profiles",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "linkedIn_url",
                table: "candidate_profiles",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "last_name",
                table: "candidate_profiles",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "github_url",
                table: "candidate_profiles",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "first_name",
                table: "candidate_profiles",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "avatar_url",
                table: "candidate_profiles",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "about_me",
                table: "candidate_profiles",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
