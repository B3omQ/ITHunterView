using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeMasterDataAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "normalized_name",
                table: "skills",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "normalized_name",
                table: "majors",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Backfill normalized_name
            migrationBuilder.Sql(@"
                UPDATE skills 
                SET normalized_name = REGEXP_REPLACE(
                    LOWER(
                        REPLACE(
                            REPLACE(
                                REPLACE(TRIM(name), 'c#', 'csharp'),
                                'c++', 'cplusplus'
                            ),
                            '.net', 'dotnet'
                        )
                    ),
                    '[^a-z0-9]',
                    '',
                    'g'
                );
            ");

            migrationBuilder.Sql(@"
                UPDATE majors 
                SET normalized_name = REGEXP_REPLACE(
                    LOWER(
                        REPLACE(
                            REPLACE(
                                REPLACE(TRIM(name), 'c#', 'csharp'),
                                'c++', 'cplusplus'
                            ),
                            '.net', 'dotnet'
                        )
                    ),
                    '[^a-z0-9]',
                    '',
                    'g'
                );
            ");

            // Drop old unique constraint from PostgreSQL
            migrationBuilder.Sql("ALTER TABLE majors DROP CONSTRAINT IF EXISTS majors_code_key;");

            migrationBuilder.CreateIndex(
                name: "IX_skills_normalized_name",
                table: "skills",
                column: "normalized_name");

            migrationBuilder.CreateIndex(
                name: "IX_majors_code",
                table: "majors",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_majors_normalized_name",
                table: "majors",
                column: "normalized_name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_skills_normalized_name",
                table: "skills");

            migrationBuilder.DropIndex(
                name: "IX_majors_code",
                table: "majors");

            migrationBuilder.DropIndex(
                name: "IX_majors_normalized_name",
                table: "majors");

            migrationBuilder.DropColumn(
                name: "normalized_name",
                table: "skills");

            migrationBuilder.DropColumn(
                name: "normalized_name",
                table: "majors");

            // Restore unique constraint
            migrationBuilder.Sql("ALTER TABLE majors ADD CONSTRAINT majors_code_key UNIQUE (code);");
        }
    }
}
