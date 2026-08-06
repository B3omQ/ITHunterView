using ITHunterview.Service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ITHunterviewContext))]
    [Migration("20260731100000_AddCvAnalysisPromptManagement")]
    public partial class AddCvAnalysisPromptManagement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "Prompts" ("Id", "PromptKey", "Description", "CreatedAt")
                VALUES
                    ('a4e8c2b1-6ad4-4e32-9c11-000000000001'::uuid, 'CV_ANALYSIS_SYSTEM', 'System prompt for CV analysis extraction', CURRENT_TIMESTAMP),
                    ('a4e8c2b1-6ad4-4e32-9c11-000000000003'::uuid, 'CV_ANALYSIS_USER', 'User prompt template for CV analysis extraction', CURRENT_TIMESTAMP);

                INSERT INTO "PromptVersions" ("Id", "PromptId", "VersionTag", "Content", "ModelConfig", "IsActive", "CreatedBy", "CreatedAt")
                VALUES
                    (
                        'a4e8c2b1-6ad4-4e32-9c11-000000000002'::uuid,
                        'a4e8c2b1-6ad4-4e32-9c11-000000000001'::uuid,
                        'v1.0.0',
                        $cv_system$
                        You are an expert ATS (Applicant Tracking System) CV parser.
                        Your task is to extract key information from the raw text of a candidate's CV and format it STRICTLY as a valid JSON object.
                        Do not include any markdown formatting (like ```json), introduction, or conclusion. Just the raw JSON object.

                        CRITICAL RULE: DO NOT SUMMARIZE in the `verbatim_sections`. You MUST copy the text verbatim (word-for-word) from the CV into the respective fields, especially for experience details and project bullet points. Loss of information is strictly forbidden.
                        Retain all numbers, percentages, metrics, and technologies exactly as they appear.

                        The JSON MUST have the exact following schema with two main branches (`verbatim_sections` and `matching_metrics`):
                        {
                          "verbatim_sections": {
                            "personal_info": {
                              "name": "",
                              "title": "",
                              "summary": ""
                            },
                            "education": [
                              {
                                "institution": "",
                                "degree": "",
                                "major": "",
                                "timeline": ""
                              }
                            ],
                            "languages": [
                              {
                                "language": "",
                                "certifications_or_level": ""
                              }
                            ],
                            "skills_section": [
                              "A list of skills that are ONLY listed in a standalone 'Skills' section. Do not include skills that only appear in project descriptions."
                            ],
                            "professional_experience_and_projects": [
                              {
                                "company_or_project_name": "",
                                "role": "",
                                "timeline": "",
                                "details_and_responsibilities": [
                                  "Copy verbatim bullet point 1",
                                  "Copy verbatim bullet point 2"
                                ],
                                "technologies_used": ["List of technologies explicitly mentioned within this specific project/role"]
                              }
                            ],
                            "certifications_and_awards": [
                              "Award 1", "Cert 2"
                            ],
                            "other_information": "Any leftover text that doesn't fit above"
                          },
                          "matching_metrics": {
                            "job_titles_normalized": ["Primary job title 1", "Job title 2"],
                            "skills_normalized": ["Skill 1", "Skill 2", "Tool 3"],
                            "total_years_exp": 0,
                            "domains": ["Finance", "E-commerce"]
                          }
                        }

                        If any information is missing or cannot be deduced, provide an empty array [] or empty string "", but the keys must always be present.
                        Ensure the output is 100% valid JSON.
                        $cv_system$,
                        $cv_system_config${"contract":"cv-analysis/v1","role":"system"}$cv_system_config$,
                        true,
                        '00000000-0000-0000-0000-000000000000'::uuid,
                        CURRENT_TIMESTAMP
                    ),
                    (
                        'a4e8c2b1-6ad4-4e32-9c11-000000000004'::uuid,
                        'a4e8c2b1-6ad4-4e32-9c11-000000000003'::uuid,
                        'v1.0.0',
                        $cv_user$
                        Extract the following CV into the required JSON format.

                        --- CV TEXT ---
                        [CV_TEXT]
                        ----------------

                        OUTPUT ONLY VALID JSON:
                        $cv_user$,
                        $cv_user_config${"contract":"cv-analysis/v1","role":"user"}$cv_user_config$,
                        true,
                        '00000000-0000-0000-0000-000000000000'::uuid,
                        CURRENT_TIMESTAMP
                    );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "PromptVersions"
                WHERE "Id" IN (
                    'a4e8c2b1-6ad4-4e32-9c11-000000000002'::uuid,
                    'a4e8c2b1-6ad4-4e32-9c11-000000000004'::uuid);

                DELETE FROM "Prompts"
                WHERE "Id" IN (
                    'a4e8c2b1-6ad4-4e32-9c11-000000000001'::uuid,
                    'a4e8c2b1-6ad4-4e32-9c11-000000000003'::uuid);
                """);
        }
    }
}
