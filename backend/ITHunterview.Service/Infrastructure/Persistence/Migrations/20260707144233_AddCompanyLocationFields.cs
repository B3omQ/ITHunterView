using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyLocationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "detailed_location",
                table: "companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "latitude",
                table: "companies",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "longitude",
                table: "companies",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_detailed_location",
                table: "companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "pending_latitude",
                table: "companies",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "pending_longitude",
                table: "companies",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_province_code",
                table: "companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "province_code",
                table: "companies",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "detailed_location",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "pending_detailed_location",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "pending_latitude",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "pending_longitude",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "pending_province_code",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "province_code",
                table: "companies");
        }
    }
}
