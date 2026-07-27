using ITHunterview.Service.Helpers;
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
    }
}
