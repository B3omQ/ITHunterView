using ITHunterview.Domain.Entities;
using ITHunterview.Service.Helpers;
using Xunit;

namespace ITHunterview.Service.Tests.Helpers
{
    public class JdTextHelperTests
    {
        [Fact]
        public void BuildRawText_WithStructuredWorkLocation_DoesNotOutputRawJson()
        {
            var job = new JobPostings
            {
                Title = "Senior C# Developer",
                Description = "Develop web API services.",
                Requirements = "5+ years C# experience.",
                Benefits = "Health insurance, 13th month salary.",
                IncomeText = "Negotiable ($2000 - $3000)",
                WorkLocationText = "{\"version\":1,\"workLocation\":\"Hanoi Office\",\"workingHours\":\"Mon-Fri\",\"howToApply\":\"Click Apply\"}"
            };

            var rawText = JdTextHelper.BuildRawText(job);

            Assert.Contains("Title: Senior C# Developer", rawText);
            Assert.Contains("Work Location: Hanoi Office", rawText);
            Assert.Contains("Working Hours: Mon-Fri", rawText);
            Assert.Contains("How to Apply: Click Apply", rawText);
            Assert.DoesNotContain("\"version\":1", rawText);
        }

        [Fact]
        public void BuildRawText_WithLegacyWorkLocation_OutputsLegacyLocationText()
        {
            var job = new JobPostings
            {
                Title = "Frontend Developer",
                WorkLocationText = "125 Hoang Ngan, Hanoi"
            };

            var rawText = JdTextHelper.BuildRawText(job);

            Assert.Contains("Title: Frontend Developer", rawText);
            Assert.Contains("Work Location: 125 Hoang Ngan, Hanoi", rawText);
        }
    }
}
