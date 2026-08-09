using FluentAssertions;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public sealed class JdMatchReportReaderTests
{
    private readonly JdMatchReportReader _reader = new();

    [Fact]
    public void Read_V4StructuredResult_PreservesGroupsEvidenceAndPercentScore()
    {
        const string details = """
            {
              "contract":"jd-matching/v4",
              "jdFit":{
                "scorePercent":81.8,
                "resultCode":"SUITABLE",
                "resultLabel":"Suitable",
                "narrative":"Good fit",
                "requirementGroups":[{
                  "groupId":"g1",
                  "sourceRequirementId":"req-1",
                  "intent":"backend development",
                  "operator":"one_of",
                  "minSatisfied":1,
                  "importance":"must_have",
                  "sourceSection":"requirements",
                  "requirementVerbatim":"Java or C#",
                  "groupScore":0.75,
                  "selectedItemIds":["g1:i2"],
                  "isCriticalGap":false,
                  "items":[{
                    "itemId":"g1:i1",
                    "normalizedText":"Java",
                    "detailVerbatim":"Java",
                    "rawMention":"Java",
                    "category":"tech_skill",
                    "score":0.5,
                    "handlerCode":"H_TECH_PARTIAL",
                    "reasoning":"Used Java in Project A.",
                    "evidence":[{"quotation":"Built Java API","section":"projects"}],
                    "isCriticalGap":false
                  }]
                }],
                "criticalGaps":[{
                  "code":"CORE_TECH_MISMATCH",
                  "scope":"group",
                  "groupId":"g1",
                  "requirement":"Java or C#",
                  "reasoning":"No production evidence was found.",
                  "evidence":[{"quotation":"Student project only","section":"projects"}]
                }],
                "warningFlags":["NOTICE"]
              }
            }
            """;

        var report = _reader.Read(details, 81.8m, "AI");

        report.ReportKind.Should().Be("structured");
        report.MatchMethod.Should().Be("one_to_one_ai");
        report.SchemaVersion.Should().Be("jd-matching/v4");
        report.ScorePercent.Should().Be(81.8m);
        report.RequirementGroups.Should().ContainSingle();
        report.RequirementGroups[0].Operator.Should().Be("one_of");
        report.RequirementGroups[0].Items[0].Evidence.Should().ContainSingle()
            .Which.Quotation.Should().Be("Built Java API");
        report.CriticalGaps.Should().ContainSingle();
        report.CriticalGaps[0].Evidence.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Quotation = "Student project only",
            Section = "projects"
        });
        report.WarningFlags.Should().Equal("NOTICE");
    }

    [Fact]
    public void Read_CurrentStructuredResult_UsesArrayOrderAndDoesNotInventLegacyMetadata()
    {
        const string details = """
            {
              "contract":"jd-matching/v3",
              "jdFit":{
                "score":72.5,
                "result":"Suitable",
                "narrative":"Current report",
                "requirementGroups":[{
                  "groupId":"g1",
                  "operator":"all_of",
                  "minSatisfied":2,
                  "importance":"must_have",
                  "handlerScore":0.5,
                  "selectedItemIds":["g1:i1","g1:i2"],
                  "items":[
                    {"itemId":"g1:i2","normalizedText":"SQL","category":"tech_skill","handlerScore":0.5,"handlerCode":"H_TECH_03","reasoning":"Some SQL","evidence":["Used SQL"]},
                    {"itemId":"g1:i1","normalizedText":"Java","category":"tech_skill","handlerScore":0.5,"handlerCode":"H_TECH_03","reasoning":"Some Java","evidence":["Used Java"]}
                  ]
                }],
                "criticalGaps":[]
              }
            }
            """;

        var report = _reader.Read(details, 72.5m, "AI");

        report.ScorePercent.Should().Be(72.5m);
        report.RequirementGroups[0].SourceRequirementId.Should().BeNull();
        report.RequirementGroups[0].Intent.Should().BeNull();
        report.RequirementGroups[0].Items.Select(item => item.ItemId)
            .Should().Equal("g1:i2", "g1:i1");
        report.RequirementGroups[0].Items[0].Evidence.Should().ContainSingle()
            .Which.Quotation.Should().Be("Used SQL");
        report.RequirementGroups[0].Items[0].Evidence[0].Section.Should().BeNull();
    }

    [Fact]
    public void Read_RawTextFallback_ReturnsValidOverviewWithoutRequirementSemantics()
    {
        const string details = """
            {"contract":"jd-matching/raw-text-v1","jdFit":{"score":64.2,"result":"Suitable","narrative":"Raw JD overview","requirementGroups":[]}}
            """;

        var report = _reader.Read(details, 64.2m, "AI");

        report.ReportKind.Should().Be("raw_text_fallback");
        report.MatchMethod.Should().Be("raw_text_ai");
        report.ScorePercent.Should().Be(64.2m);
        report.RequirementGroups.Should().BeEmpty();
        report.Narrative.Should().Be("Raw JD overview");
    }

    [Theory]
    [InlineData("Hardcode", "{\"Method\":\"HardcodeV3\",\"FinalScore\":0.818}", "hardcode")]
    [InlineData("AI", "{\"TitleScore\":0.8,\"SkillsScore\":0.9,\"ExperienceScore\":0.7,\"DomainScore\":0.6,\"FinalScore\":0.818}", "vector")]
    public void Read_KnownFractionalLegacyMethod_NormalizesScoreExactlyOnce(
        string matchType,
        string details,
        string expectedMethod)
    {
        var report = _reader.Read(details, 0.818m, matchType);

        report.ReportKind.Should().Be("legacy_summary");
        report.MatchMethod.Should().Be(expectedMethod);
        report.ScorePercent.Should().Be(81.8m);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("{not-json")]
    public void Read_MalformedOrMissingHistoricalDetails_ReturnsSafeNonNullSummary(string? details)
    {
        var report = _reader.Read(details, 81.8m, "AI");

        report.Should().NotBeNull();
        report.ReportKind.Should().Be("legacy_summary");
        report.MatchMethod.Should().Be("legacy_unknown");
        report.ScorePercent.Should().Be(81.8m);
        report.RequirementGroups.Should().BeEmpty();
    }
}
