using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGranularEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "embedding",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "embedding",
                table: "cvs");

            migrationBuilder.AddColumn<Vector>(
                name: "domain_embedding",
                table: "job_postings",
                type: "vector(768)",
                nullable: true);

            migrationBuilder.AddColumn<Vector>(
                name: "experience_embedding",
                table: "job_postings",
                type: "vector(768)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "parsed_data",
                table: "job_postings",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Vector>(
                name: "skills_embedding",
                table: "job_postings",
                type: "vector(768)",
                nullable: true);

            migrationBuilder.AddColumn<Vector>(
                name: "title_embedding",
                table: "job_postings",
                type: "vector(768)",
                nullable: true);

            migrationBuilder.AddColumn<Vector>(
                name: "domain_embedding",
                table: "cvs",
                type: "vector(768)",
                nullable: true);

            migrationBuilder.AddColumn<Vector>(
                name: "experience_embedding",
                table: "cvs",
                type: "vector(768)",
                nullable: true);

            migrationBuilder.AddColumn<Vector>(
                name: "skills_embedding",
                table: "cvs",
                type: "vector(768)",
                nullable: true);

            migrationBuilder.AddColumn<Vector>(
                name: "title_embedding",
                table: "cvs",
                type: "vector(768)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "domain_embedding",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "experience_embedding",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "parsed_data",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "skills_embedding",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "title_embedding",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "domain_embedding",
                table: "cvs");

            migrationBuilder.DropColumn(
                name: "experience_embedding",
                table: "cvs");

            migrationBuilder.DropColumn(
                name: "skills_embedding",
                table: "cvs");

            migrationBuilder.DropColumn(
                name: "title_embedding",
                table: "cvs");

            migrationBuilder.AddColumn<Vector>(
                name: "embedding",
                table: "job_postings",
                type: "vector(768)",
                nullable: false);

            migrationBuilder.AddColumn<Vector>(
                name: "embedding",
                table: "cvs",
                type: "vector(768)",
                nullable: false);
        }
    }
}
