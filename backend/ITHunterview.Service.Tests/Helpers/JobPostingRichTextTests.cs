using System;
using ITHunterview.Service.Utils;
using Xunit;

namespace ITHunterview.Service.Tests.Helpers
{
    public class JobPostingRichTextTests
    {
        [Fact]
        public void NormalizeForStorage_CanonicalizesListsAndPreservesVisibleText()
        {
            var result = JobPostingRichText.NormalizeForStorage(" * React\r\n+ Node.js\r\n1. PostgreSQL ");

            Assert.Equal("- React\n- Node.js\n1. PostgreSQL", result.StoredMarkdown);
            Assert.Equal("React\nNode.js\nPostgreSQL", result.PlainText);
        }

        [Fact]
        public void NormalizeForStorage_RepairsMalformedListMarkerBeforeInlineFormatting()
        {
            var result = JobPostingRichText.NormalizeForStorage("-**Lead backend delivery**");

            Assert.Equal("- **Lead backend delivery**", result.StoredMarkdown);
            Assert.Equal("Lead backend delivery", result.PlainText);
        }

        [Fact]
        public void ToPlainText_RemovesSupportedFormattingButPreservesTechnologyPunctuation()
        {
            var plain = JobPostingRichText.ToPlainText("**React**\n_Node.js_\n++CI/CD++\nC++\nC#\nsome_text_here\na < b");

            Assert.Equal("React\nNode.js\nCI/CD\nC++\nC#\nsome_text_here\na < b", plain);
        }

        [Fact]
        public void ToPlainText_RemovesFormattingThatSpansMultipleLines()
        {
            var plain = JobPostingRichText.ToPlainText("**Own the API\nlifecycle**");

            Assert.Equal("Own the API\nlifecycle", plain);
        }

        [Theory]
        [InlineData("****")]
        [InlineData("++++")]
        [InlineData("-")]
        [InlineData(" \r\n\t ")]
        public void HasVisibleText_WhenOnlyFormattingOrWhitespace_ReturnsFalse(string value)
        {
            Assert.False(JobPostingRichText.HasVisibleText(value));
        }

        [Fact]
        public void NormalizeForStorage_WhenRawHtmlIsPresent_ThrowsSafeArgumentException()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                JobPostingRichText.NormalizeForStorage("<script>alert(1)</script>"));

            Assert.Contains("must not contain raw HTML", exception.Message);
        }
    }
}
