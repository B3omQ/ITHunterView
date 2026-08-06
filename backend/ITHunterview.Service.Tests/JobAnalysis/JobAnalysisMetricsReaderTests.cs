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
        }
    }
}
