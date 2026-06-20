using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrigramSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_users_email_trgm ON users USING gin (email gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_candidate_profiles_first_name_trgm ON candidate_profiles USING gin (first_name gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_candidate_profiles_last_name_trgm ON candidate_profiles USING gin (last_name gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_recruiter_profiles_full_name_trgm ON recruiter_profiles USING gin (full_name gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_companies_name_trgm ON companies USING gin (name gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_user_activity_logs_actor_email_trgm ON user_activity_logs USING gin (actor_email gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_user_activity_logs_action_trgm ON user_activity_logs USING gin (action gin_trgm_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_user_activity_logs_action_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_user_activity_logs_actor_email_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_companies_name_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_recruiter_profiles_full_name_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_candidate_profiles_last_name_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_candidate_profiles_first_name_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_users_email_trgm;");
        }
    }
}
