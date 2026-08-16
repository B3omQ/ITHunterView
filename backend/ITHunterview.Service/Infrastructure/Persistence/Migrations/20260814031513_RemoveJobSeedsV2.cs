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
        -- 1. Gỡ bỏ foreign keys tham chiếu vòng từ job_postings sang job_analysis_runs
        UPDATE ""job_postings"" 
        SET ""active_analysis_run_id"" = NULL, ""effective_analysis_run_id"" = NULL 
        WHERE ""id"" = ANY(target_job_ids);

        -- 2. Xóa các bảng phụ thuộc
        DELETE FROM ""job_skill_requirements"" WHERE ""job_id"" = ANY(target_job_ids);
        DELETE FROM ""cv_job_match_scores"" WHERE ""job_id"" = ANY(target_job_ids);
        DELETE FROM ""user_saved_jobs"" WHERE ""job_id"" = ANY(target_job_ids);
        DELETE FROM ""job_applications"" WHERE ""job_id"" = ANY(target_job_ids);
        DELETE FROM ""job_promotions"" WHERE ""job_id"" = ANY(target_job_ids);
        DELETE FROM ""job_reviews"" WHERE ""job_id"" = ANY(target_job_ids);
        DELETE FROM ""job_skill_decisions"" WHERE ""job_analysis_run_id"" IN (
            SELECT ""id"" FROM ""job_analysis_runs"" WHERE ""job_id"" = ANY(target_job_ids)
        );
        DELETE FROM ""job_analysis_runs"" WHERE ""job_id"" = ANY(target_job_ids);

        -- 3. Xóa các bài đăng tuyển dụng
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
