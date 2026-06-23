using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruiterCompanyRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_recruiter_profiles_company_id",
                table: "recruiter_profiles",
                column: "company_id");

            migrationBuilder.AddForeignKey(
                name: "FK_recruiter_profiles_companies_company_id",
                table: "recruiter_profiles",
                column: "company_id",
                principalTable: "companies",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_recruiter_profiles_companies_company_id",
                table: "recruiter_profiles");

            migrationBuilder.DropIndex(
                name: "IX_recruiter_profiles_company_id",
                table: "recruiter_profiles");
        }
    }
}
