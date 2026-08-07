using ITHunterview.Service.Utils;
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
        public void Read_MissingMetric_IsUnavailableButPresentEmptyArrayRemainsAvailable()
        {
            const string partial = """
                {"schema_version":"cv-analysis/v2","matching_metrics":{"job_titles_normalized":[],"skills_normalized":["c#"],"total_years_exp":3}}
                """;

            var metrics = JobAnalysisMetricsReader.Read(partial);

            Assert.True(metrics.TitleAvailable);
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

            Assert.True(metrics.TitleAvailable);
            Assert.False(metrics.SkillsAvailable);
            Assert.True(metrics.ExperienceAvailable);
            Assert.False(metrics.DomainsAvailable);
        }
    }
}
