using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveJobSeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""job_skill_requirements""
                WHERE ""job_id"" IN (
                    SELECT ""id"" FROM ""job_postings""
                    WHERE (""title"" LIKE '%(RealisticSeedV2)%' OR ""title"" LIKE '%(RealisticSeed)%')
                       OR (""income_text"" = 'Mức lương cạnh tranh, thỏa thuận theo năng lực' AND ""work_location_text"" = 'Làm việc tại văn phòng công ty')
                );

                DELETE FROM ""job_postings"" 
                WHERE (""title"" LIKE '%(RealisticSeedV2)%' OR ""title"" LIKE '%(RealisticSeed)%')
                   OR (""income_text"" = 'Mức lương cạnh tranh, thỏa thuận theo năng lực' AND ""work_location_text"" = 'Làm việc tại văn phòng công ty');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
