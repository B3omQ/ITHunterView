using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HierarchicalMajors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "parent_id",
                table: "majors",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_majors_parent_id",
                table: "majors",
                column: "parent_id");

            migrationBuilder.AddForeignKey(
                name: "FK_majors_majors_parent_id",
                table: "majors",
                column: "parent_id",
                principalTable: "majors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_majors_majors_parent_id",
                table: "majors");

            migrationBuilder.DropIndex(
                name: "IX_majors_parent_id",
                table: "majors");

            migrationBuilder.DropColumn(
                name: "parent_id",
                table: "majors");
        }
    }
}
