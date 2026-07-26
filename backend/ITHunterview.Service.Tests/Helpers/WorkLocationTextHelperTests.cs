using ITHunterview.Service.Helpers;
using Xunit;

namespace ITHunterview.Service.Tests.Helpers
{
    public class WorkLocationTextHelperTests
    {
        [Fact]
        public void FormatForAi_NullOrEmpty_ReturnsEmptyString()
        {
            Assert.Equal(string.Empty, WorkLocationTextHelper.FormatForAi(null));
            Assert.Equal(string.Empty, WorkLocationTextHelper.FormatForAi("   "));
        }

        [Fact]
        public void FormatForAi_LegacyString_ReturnsWorkLocationLabelWithRawText()
        {
            var raw = "Hanoi, Vietnam";
            var result = WorkLocationTextHelper.FormatForAi(raw);
            Assert.Equal("Work Location: Hanoi, Vietnam", result);
        }

        [Fact]
        public void FormatForAi_ValidJsonV1_FormatsSubsectionsCorrectly()
        {
            var json = "{\"version\":1,\"workLocation\":\"Hanoi: 125 Hoang Ngan\",\"workingHours\":\"Mon - Fri (09:00 - 18:00)\",\"howToApply\":\"Apply online via button below\"}";
            var result = WorkLocationTextHelper.FormatForAi(json);

            var expected = "Work Location: Hanoi: 125 Hoang Ngan\nWorking Hours: Mon - Fri (09:00 - 18:00)\nHow to Apply: Apply online via button below";
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
        }

        [Fact]
        public void FormatForAi_ValidJsonV1_OmitsEmptySubsections()
        {
            var json = "{\"version\":1,\"workLocation\":\"Hanoi: 125 Hoang Ngan\",\"workingHours\":\"\",\"howToApply\":\"Apply online\"}";
            var result = WorkLocationTextHelper.FormatForAi(json);

            var expected = "Work Location: Hanoi: 125 Hoang Ngan\nHow to Apply: Apply online";
            Assert.Equal(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
        }

        [Fact]
        public void FormatForAi_MalformedJson_FallsBackToLegacyRawText()
        {
            var malformed = "{version:1, workLocation: broken json}";
            var result = WorkLocationTextHelper.FormatForAi(malformed);

            Assert.Equal("Work Location: {version:1, workLocation: broken json}", result);
        }
    }
}
