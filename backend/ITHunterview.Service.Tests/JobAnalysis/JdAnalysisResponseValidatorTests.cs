using ITHunterview.Service.Utils;
using ITHunterview.Service.Utils;
using Xunit;

namespace ITHunterview.Service.Tests.JobAnalysis
{
    public class JdAnalysisResponseValidatorTests
    {
        private readonly JdAnalysisResponseValidator _validator = new();

        [Fact]
        public void Validate_WithValidV2Json_ReturnsTypedResult()
        {
            string validJson = """
            {
              "schema_version": "jd-analysis/v2",
              "matching_metrics": {
                "job_titles_normalized": ["backend developer"],
                "skills_normalized": [
                  {
                    "name": "c#",
                    "category": "tech_skill",
                    "raw_mention": "C#",
                    "source_section": "requirements",
                    "evidence": "3 years C# experience",
                    "confidence": 0.95
                  }
                ],
                "total_years_exp": 3,
                "domains": ["fintech"],
                "requirements_list": [
                  {
                    "category": "tech_skill",
                    "importance": "must_have",
                    "skill_name": "c#",
                    "detail_verbatim": "3 years C# experience",
                    "raw_mention": "C#",
                    "source_section": "requirements",
                    "evidence": "3 years C# experience",
                    "confidence": 0.95
                  }
                ]
              }
            }
            """;

            var input = new JobAnalysisInputSnapshot
            {
                Title = "Backend Dev",
                Description = "Join our team",
                Requirements = "Must have 3 years C# experience"
            };

            var result = _validator.Validate(validJson, input);

            Assert.True(result.IsValid);
            Assert.NotNull(result.Data);
            Assert.Single(result.Data!.SkillsNormalized);
            Assert.Equal("c#", result.Data.SkillsNormalized[0].Name);
        }

        [Fact]
        public void Validate_WithMarkdownFence_UsesSingleJsonObjectOnly()
        {
            string markdownJson = """
            ```json
            {
              "schema_version": "jd-analysis/v2",
              "matching_metrics": {
                "job_titles_normalized": ["dev"],
                "skills_normalized": [],
                "total_years_exp": 1,
                "domains": [],
                "requirements_list": []
              }
            }
            ```
            """;

            var input = new JobAnalysisInputSnapshot();
            var result = _validator.Validate(markdownJson, input);

            Assert.True(result.IsValid);
            Assert.Equal("jd-analysis/v2", result.Data!.SchemaVersion);
        }

        [Fact]
        public void Validate_WithMissingMatchingMetrics_ReturnsInvalidSchema()
        {
            string invalidJson = """
            {
              "schema_version": "jd-analysis/v2"
            }
            """;

            var input = new JobAnalysisInputSnapshot();
            var result = _validator.Validate(invalidJson, input);

            Assert.False(result.IsValid);
            Assert.Equal("MISSING_MATCHING_METRICS", result.FailureCode);
        }

        [Fact]
        public void Validate_WithEvidenceNotInInput_RejectsProviderOutput()
        {
            const string json = """
            {
              "schema_version":"jd-analysis/v2",
              "matching_metrics":{
                "job_titles_normalized":[],
                "skills_normalized":[{"name":"react","category":"tech_skill","raw_mention":"React","source_section":"requirements","evidence":"React"}],
                "total_years_exp":0,
                "domains":[],
                "requirements_list":[{"category":"tech_skill","importance":"must_have","skill_name":"react","detail_verbatim":"React","raw_mention":"React","source_section":"requirements","evidence":"React"}]
              }
            }
            """;

            var result = _validator.Validate(json, new JobAnalysisInputSnapshot { Requirements = "C# is required" });

            Assert.False(result.IsValid);
            Assert.Equal("EVIDENCE_NOT_IN_INPUT", result.FailureCode);
        }

        [Fact]
        public void Validate_DerivesDeduplicatedSkillProjectionAndKeepsNiceToHaveImportance()
        {
            const string json = """
            {
              "schema_version":"jd-analysis/v2",
              "matching_metrics":{
                "job_titles_normalized":["Backend Developer", " backend   developer "],
                "skills_normalized":[{"name":"c#","category":"tech_skill","raw_mention":"C#","source_section":"requirements","evidence":"C# is preferred"}],
                "total_years_exp":0,
                "domains":["FinTech", " fintech "],
                "requirements_list":[
                  {"category":"tech_skill","importance":"nice_to_have","skill_name":"C#","detail_verbatim":"C# is preferred","raw_mention":"C#","source_section":"requirements","evidence":"C# is preferred"},
                  {"category":"tech_skill","importance":"nice_to_have","skill_name":"C#","detail_verbatim":"C# is preferred","raw_mention":"C#","source_section":"requirements","evidence":"C# is preferred"}
                ]
              }
            }
            """;

            var result = _validator.Validate(json, new JobAnalysisInputSnapshot { Requirements = "C# is preferred" });

            Assert.True(result.IsValid);
            Assert.Single(result.Data!.RequirementsList);
            Assert.Single(result.Data.SkillsNormalized);
            Assert.Equal("nice_to_have", result.Data.SkillsNormalized[0].Importance);
            Assert.Equal(new[] { "backend developer" }, result.Data.JobTitlesNormalized);
            Assert.Equal(new[] { "fintech" }, result.Data.Domains);
        }

        [Fact]
        public void Validate_WithMissingRequiredArray_RejectsProviderOutput()
        {
            const string json = """
            {"schema_version":"jd-analysis/v2","matching_metrics":{"job_titles_normalized":[],"total_years_exp":0,"domains":[],"requirements_list":[]}}
            """;

            var result = _validator.Validate(json, new JobAnalysisInputSnapshot());

            Assert.False(result.IsValid);
            Assert.Equal("MISSING_REQUIRED_ARRAY", result.FailureCode);
        }

        [Fact]
        public void Validate_V3OneOfGroup_PreservesAlternativeSemantics()
        {
            const string json = """
            {
              "schema_version":"jd-analysis/v3",
              "matching_metrics":{
                "job_titles_normalized":["frontend developer"],
                "skills_normalized":[],
                "total_years_exp":0,
                "domains":[],
                "requirement_groups":[{
                  "operator":"one_of",
                  "min_satisfied":1,
                  "importance":"must_have",
                  "items":[
                    {"category":"tech_skill","skill_name":"react","detail_verbatim":"React, Angular, or Vue","raw_mention":"React","source_section":"requirements","evidences":["React, Angular, or Vue"]},
                    {"category":"tech_skill","skill_name":"angular","detail_verbatim":"React, Angular, or Vue","raw_mention":"Angular","source_section":"requirements","evidences":["React, Angular, or Vue"]},
                    {"category":"tech_skill","skill_name":"vue","detail_verbatim":"React, Angular, or Vue","raw_mention":"Vue","source_section":"requirements","evidences":["React, Angular, or Vue"]}
                  ]
                }]
              }
            }
            """;

            var result = _validator.Validate(json, new JobAnalysisInputSnapshot
            {
                Requirements = "Candidates need React, Angular, or Vue."
            });

            Assert.True(result.IsValid);
        }
    }
}
