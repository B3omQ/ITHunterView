using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCvsNavigationProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_cvs_user_id",
                table: "cvs",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_cvs_users_user_id",
                table: "cvs",
                column: "user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cvs_users_user_id",
                table: "cvs");

            migrationBuilder.DropIndex(
                name: "IX_cvs_user_id",
                table: "cvs");
        }
    }
}
