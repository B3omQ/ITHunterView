using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveJobSeedsV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$ 
DECLARE
    target_job_ids UUID[];
BEGIN
    SELECT array_agg(id) INTO target_job_ids
    FROM ""job_postings""
    WHERE ""company_id"" IN (
        SELECT ""id"" FROM ""companies"" WHERE ""name"" IN ('ITHunterView Corp', 'FPT Software', 'VNG Corporation')
    ) OR ""title"" LIKE '%(RealisticSeed)%' OR ""title"" LIKE '%(RealisticSeedV2)%';

    IF target_job_ids IS NOT NULL THEN
        DELETE FROM ""job_skill_requirements"" WHERE ""job_id"" = ANY(target_job_ids);
        DELETE FROM ""cv_job_match_scores"" WHERE ""job_id"" = ANY(target_job_ids);
        DELETE FROM ""user_saved_jobs"" WHERE ""job_id"" = ANY(target_job_ids);
        DELETE FROM ""job_applications"" WHERE ""job_id"" = ANY(target_job_ids);
        DELETE FROM ""job_promotions"" WHERE ""job_id"" = ANY(target_job_ids);
        DELETE FROM ""job_reviews"" WHERE ""job_id"" = ANY(target_job_ids);
        DELETE FROM ""job_analysis_runs"" WHERE ""job_id"" = ANY(target_job_ids);

        DELETE FROM ""job_postings"" WHERE ""id"" = ANY(target_job_ids);
    END IF;
END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
