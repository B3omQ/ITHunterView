using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOptimizeSessionForStandalone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "MatchSessionId",
                table: "OptimizeSessions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "AnalysisResultJson",
                table: "OptimizeSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CvFileName",
                table: "OptimizeSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CvId",
                table: "OptimizeSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OverallScore",
                table: "OptimizeSessions",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "OptimizeSessions",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalysisResultJson",
                table: "OptimizeSessions");

            migrationBuilder.DropColumn(
                name: "CvFileName",
                table: "OptimizeSessions");

            migrationBuilder.DropColumn(
                name: "CvId",
                table: "OptimizeSessions");

            migrationBuilder.DropColumn(
                name: "OverallScore",
                table: "OptimizeSessions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "OptimizeSessions");

            migrationBuilder.AlterColumn<Guid>(
                name: "MatchSessionId",
                table: "OptimizeSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
