using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateJobLocationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "location",
                table: "job_postings",
                newName: "detailed_location");

            migrationBuilder.AddColumn<decimal>(
                name: "latitude",
                table: "job_postings",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "longitude",
                table: "job_postings",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "province_code",
                table: "job_postings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "latitude",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "province_code",
                table: "job_postings");

            migrationBuilder.RenameColumn(
                name: "detailed_location",
                table: "job_postings",
                newName: "location");
        }
    }
}
