using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobAnalysisWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "active_analysis_run_id",
                table: "job_postings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "analysis_input_hash",
                table: "job_postings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "analysis_revision",
                table: "job_postings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "job_analysis_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    input_revision = table.Column<int>(type: "integer", nullable: false),
                    input_hash = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    system_prompt_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_prompt_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schema_version = table.Column<string>(type: "text", nullable: false),
                    raw_input_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    raw_analysis_json = table.Column<string>(type: "jsonb", nullable: true),
                    effective_analysis_json = table.Column<string>(type: "jsonb", nullable: true),
                    validation_errors_json = table.Column<string>(type: "jsonb", nullable: true),
                    provider_name = table.Column<string>(type: "text", nullable: true),
                    model_name = table.Column<string>(type: "text", nullable: true),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failure_code = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_analysis_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_job_analysis_runs_PromptVersions_system_prompt_version_id",
                        column: x => x.system_prompt_version_id,
                        principalTable: "PromptVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_analysis_runs_PromptVersions_user_prompt_version_id",
                        column: x => x.user_prompt_version_id,
                        principalTable: "PromptVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_analysis_runs_job_postings_job_id",
                        column: x => x.job_id,
                        principalTable: "job_postings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "job_skill_decisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_analysis_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    raw_mention = table.Column<string>(type: "text", nullable: false),
                    normalized_mention = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    importance = table.Column<string>(type: "text", nullable: false),
                    source_section = table.Column<string>(type: "text", nullable: false),
                    evidence_text = table.Column<string>(type: "text", nullable: false),
                    suggested_skill_id = table.Column<int>(type: "integer", nullable: true),
                    resolved_skill_id = table.Column<int>(type: "integer", nullable: true),
                    resolution_status = table.Column<string>(type: "text", nullable: false),
                    decision_status = table.Column<string>(type: "text", nullable: false),
                    confidence = table.Column<decimal>(type: "numeric", nullable: true),
                    decision_version = table.Column<int>(type: "integer", nullable: false),
                    decided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_skill_decisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_job_skill_decisions_job_analysis_runs_job_analysis_run_id",
                        column: x => x.job_analysis_run_id,
                        principalTable: "job_analysis_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_job_skill_decisions_skills_resolved_skill_id",
                        column: x => x.resolved_skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_skill_decisions_skills_suggested_skill_id",
                        column: x => x.suggested_skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_postings_active_analysis_run_id",
                table: "job_postings",
                column: "active_analysis_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_analysis_runs_job_id_input_hash",
                table: "job_analysis_runs",
                columns: new[] { "job_id", "input_hash" });

            migrationBuilder.CreateIndex(
                name: "IX_job_analysis_runs_job_id_input_revision",
                table: "job_analysis_runs",
                columns: new[] { "job_id", "input_revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_analysis_runs_status_created_at",
                table: "job_analysis_runs",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_job_analysis_runs_system_prompt_version_id",
                table: "job_analysis_runs",
                column: "system_prompt_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_analysis_runs_user_prompt_version_id",
                table: "job_analysis_runs",
                column: "user_prompt_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_skill_decisions_job_analysis_run_id",
                table: "job_skill_decisions",
                column: "job_analysis_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_skill_decisions_resolved_skill_id",
                table: "job_skill_decisions",
                column: "resolved_skill_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_skill_decisions_suggested_skill_id",
                table: "job_skill_decisions",
                column: "suggested_skill_id");

            migrationBuilder.AddForeignKey(
                name: "FK_job_postings_job_analysis_runs_active_analysis_run_id",
                table: "job_postings",
                column: "active_analysis_run_id",
                principalTable: "job_analysis_runs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            var systemPromptId = new Guid("8f3b6a9c-1234-4567-89ab-000000000001");
            var systemVersionId = new Guid("8f3b6a9c-1234-4567-89ab-000000000002");
            var userPromptId = new Guid("8f3b6a9c-1234-4567-89ab-000000000003");
            var userVersionId = new Guid("8f3b6a9c-1234-4567-89ab-000000000004");

            migrationBuilder.InsertData(
                table: "Prompts",
                columns: new[] { "Id", "PromptKey", "Description", "CreatedAt" },
                values: new object[,]
                {
                    { systemPromptId, "JD_ANALYSIS_V2_SYSTEM", "System prompt for V2 AI Job Analysis Extraction", DateTime.UtcNow },
                    { userPromptId, "JD_ANALYSIS_V2_USER", "User prompt template for V2 AI Job Analysis Extraction", DateTime.UtcNow }
                });

            migrationBuilder.InsertData(
                table: "PromptVersions",
                columns: new[] { "Id", "PromptId", "VersionTag", "Content", "ModelConfig", "IsActive", "CreatedBy", "CreatedAt" },
                values: new object[,]
                {
                    {
                        systemVersionId,
                        systemPromptId,
                        "v2.0.0",
                        """
                        You are an expert IT job requirement extraction AI.
                        Your task is to analyze the job input JSON and extract structured requirements, skills, and domain metrics into a valid JSON object matching schema 'jd-analysis/v2'.

                        SECURITY & EXTRACTION CONSTRAINTS:
                        1. ONLY extract information directly supported by the input text. Do NOT invent degrees, years of experience, skills, or company policies not present in input.
                        2. Evidence text MUST be a direct, verbatim substring from the input text.
                        3. Category MUST be one of: "tech_skill", "experience", "domain_knowledge", "language", "education", "soft_skill".
                        4. Importance MUST be one of: "must_have", "nice_to_have".
                        5. OUTPUT ONLY THE JSON OBJECT. DO NOT INCLUDE MARKDOWN CODE FENCES OR EXTRA TEXT.

                        JSON SCHEMA:
                        {
                          "schema_version": "jd-analysis/v2",
                          "matching_metrics": {
                            "job_titles_normalized": ["string"],
                            "skills_normalized": [
                              {
                                "name": "string (lowercase normalized skill name)",
                                "category": "tech_skill",
                                "raw_mention": "string",
                                "source_section": "requirements",
                                "evidence": "string (verbatim substring)",
                                "confidence": 0.95
                              }
                            ],
                            "total_years_exp": 0,
                            "domains": ["string"],
                            "requirements_list": [
                              {
                                "category": "tech_skill",
                                "importance": "must_have",
                                "skill_name": "string",
                                "detail_verbatim": "string",
                                "raw_mention": "string",
                                "source_section": "requirements",
                                "evidence": "string",
                                "confidence": 0.95
                              }
                            ]
                          }
                        }
                        """,
                        "{}",
                        true,
                        Guid.Empty,
                        DateTime.UtcNow
                    },
                    {
                        userVersionId,
                        userPromptId,
                        "v2.0.0",
                        """
                        Extract job analysis data from the following job input:
                        [JOB_INPUT_JSON]
                        """,
                        "{}",
                        true,
                        Guid.Empty,
                        DateTime.UtcNow
                    }
                });
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_job_postings_job_analysis_runs_active_analysis_run_id",
                table: "job_postings");

            migrationBuilder.DropTable(
                name: "job_skill_decisions");

            migrationBuilder.DropTable(
                name: "job_analysis_runs");

            migrationBuilder.DropIndex(
                name: "IX_job_postings_active_analysis_run_id",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "active_analysis_run_id",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "analysis_input_hash",
                table: "job_postings");

            migrationBuilder.DropColumn(
                name: "analysis_revision",
                table: "job_postings");
        }
    }
}
