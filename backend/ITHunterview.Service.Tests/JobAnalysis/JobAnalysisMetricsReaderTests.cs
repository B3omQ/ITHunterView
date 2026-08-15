using ITHunterview.Service.Utils;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Domain.Enums;
using Xunit;

namespace ITHunterview.Service.Tests.JobAnalysis
{
    public class JobAnalysisMetricsReaderTests
    {
        [Fact]
        public void Read_ProjectsEquivalentV1AndV2SkillNames()
        {
            const string v1 = """{"matching_metrics":{"job_titles_normalized":["developer"],"skills_normalized":["React","node.js"],"total_years_exp":2,"domains":["web"]}}""";
            const string v2 = """{"matching_metrics":{"job_titles_normalized":["developer"],"skills_normalized":[{"name":"React"},{"name":"node.js"}],"total_years_exp":2,"domains":["web"],"requirements_list":[]}}""";

            var legacy = JobAnalysisMetricsReader.Read(v1);
            var current = JobAnalysisMetricsReader.Read(v2);

            Assert.Equal(legacy.Skills, current.Skills);
            Assert.Equal(new[] { "node.js", "React" }, current.Skills);
            Assert.Equal(2, current.TotalYearsExperience);
        }

        [Fact]
        public void Read_EffectiveV1_UsesCompactStringSkillMetrics()
        {
            const string effectiveV1 = """
                {"schema_version":"jd-analysis-effective/v1","matching_metrics":{"job_titles_normalized":["Backend Developer"],"skills_normalized":["Spring Boot","Java"],"total_years_exp":3,"domains":["banking"],"requirement_groups":[]}}
                """;

            var metrics = JobAnalysisMetricsReader.Read(effectiveV1);

            Assert.Equal(new[] { "Java", "Spring Boot" }, metrics.Skills);
            Assert.True(metrics.SkillsAvailable);
        }

        [Fact]
        public void JdAnalysisMetadataReader_EffectiveV1DerivesCompleteRequirementSetFromCounts()
        {
            const string effectiveV1 = """
                {"schema_version":"jd-analysis-effective/v1","analysis_coverage":{"input_group_count":2,"accepted_group_count":2,"discarded_group_count":0,"input_item_count":3,"accepted_item_count":3,"discarded_item_count":0,"was_truncated":false}}
                """;

            var coverage = JdAnalysisMetadataReader.ReadCoverage(effectiveV1);

            Assert.NotNull(coverage);
            Assert.True(coverage!.RequirementSetComplete);
        }

        [Fact]
        public void Read_CvAnalysisV2_UsesTheRequiredStringMetricArraysForHardcodeCompatibility()
        {
            const string cvAnalysisV2 = """
                {"schema_version":"cv-analysis/v2","matching_metrics":{"job_titles_normalized":["backend developer"],"skills_normalized":["c#","asp.net core"],"total_years_exp":3,"domains":["fintech"]}}
                """;

            var metrics = JobAnalysisMetricsReader.Read(cvAnalysisV2);

            Assert.Equal(new[] { "backend developer" }, metrics.Titles);
            Assert.Equal(new[] { "asp.net core", "c#" }, metrics.Skills);
            Assert.Equal(3, metrics.TotalYearsExperience);
            Assert.Equal(new[] { "fintech" }, metrics.Domains);
            Assert.True(metrics.TitleAvailable);
            Assert.True(metrics.SkillsAvailable);
            Assert.True(metrics.ExperienceAvailable);
            Assert.True(metrics.DomainsAvailable);
        }

        [Fact]
        public void Read_MissingOrEmptyArrayMetric_IsUnavailable()
        {
            const string partial = """
                {"schema_version":"cv-analysis/v2","matching_metrics":{"job_titles_normalized":[],"skills_normalized":["c#"],"total_years_exp":3}}
                """;

            var metrics = JobAnalysisMetricsReader.Read(partial);

            Assert.False(metrics.TitleAvailable);
            Assert.True(metrics.SkillsAvailable);
            Assert.True(metrics.ExperienceAvailable);
            Assert.False(metrics.DomainsAvailable);
        }

        [Fact]
        public void Read_AnalysisCoverage_OverridesCanonicalPropertyPresence()
        {
            const string partial = """
                {
                  "schema_version":"cv-analysis/v2",
                  "matching_metrics":{"job_titles_normalized":[],"skills_normalized":[],"total_years_exp":0,"domains":[]},
                  "analysis_coverage":{"title_metrics_available":true,"skill_metrics_available":false,"experience_metric_available":true,"domain_metrics_available":false}
                }
                """;

            var metrics = JobAnalysisMetricsReader.Read(partial);

            Assert.False(metrics.TitleAvailable);
            Assert.False(metrics.SkillsAvailable);
            Assert.True(metrics.ExperienceAvailable);
            Assert.False(metrics.DomainsAvailable);
        }

        [Theory]
        [InlineData("-1")]
        [InlineData("\"three\"")]
        [InlineData("null")]
        public void Read_InvalidExperienceMetric_IsUnavailableInsteadOfBecomingZeroEvidence(string value)
        {
            var json =
                "{\"matching_metrics\":{\"job_titles_normalized\":[],\"skills_normalized\":[]," +
                "\"total_years_exp\":" + value + ",\"domains\":[]}}";

            var metrics = JobAnalysisMetricsReader.Read(json);

            Assert.Equal(0, metrics.TotalYearsExperience);
            Assert.False(metrics.ExperienceAvailable);
        }

        [Fact]
        public void JdAnalysisMetadataReader_RoundTripsCoverageAndDiagnostics()
        {
            var coverage = new JdAnalysisCoverage(2, 1, 1, 3, 2, 1, false);
            var diagnostics = new[] { new JdAnalysisDiagnostic("OUTPUT_TRUNCATED", "$") };

            var coverageJson = JdAnalysisMetadataReader.SerializeCoverage(coverage);
            var diagnosticsJson = JdAnalysisMetadataReader.SerializeDiagnostics(diagnostics);

            Assert.Equal(coverage, JdAnalysisMetadataReader.ReadCoverageJson(coverageJson));
            Assert.Equal(diagnostics, JdAnalysisMetadataReader.ReadDiagnosticsJson(diagnosticsJson));
        }
    }
}
