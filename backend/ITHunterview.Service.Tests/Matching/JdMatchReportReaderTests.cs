using FluentAssertions;
using System.Text.Json;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public sealed class JdMatchReportReaderTests
{
    private readonly JdMatchReportReader _reader = new();

    [Fact]
    public void SerializerToReader_CrossFamilyHandler_PreservesProjectionCategoryAndCanonicalHandler()
    {
        var item = new ProjectedJdRequirementItem(
            "g1:i1", "tech_skill", "java", "Java", "Java", "requirements",
            Array.Empty<string>(), null, null, 1m);
        var group = new ProjectedJdRequirementGroup(
            "g1", "all_of", 1, "must_have", new[] { item }, "requirements", "Java", "req-1", "java");
        var projection = new JdRequirementProjection("jd-analysis/v4", new[] { group }, false);
        using var json = JsonDocument.Parse("""
            {"schemaVersion":"jd-stage2/v2","scores":[
              {"reqId":"g1:i1","handlerCode":"h_exp_d04"}
            ]}
            """);
        var response = new JdMatchingResponseAdapter().Adapt(json, projection);
        var score = new JdFitScoreCalculator().Calculate(projection, response);
        var gaps = new JdCriticalGapEvaluator().Evaluate(projection, response.ItemAssessments);
        var persisted = new JdFitResultSerializer().Serialize(
            projection,
            response,
            score,
            gaps,
            new JdFitSerializationContext(Guid.NewGuid(), "v3.0.1", "semantic", "schema", 1));

        var report = _reader.Read(persisted.JsonString, persisted.FinalScore, "AI");

        report.RequirementGroups[0].Items[0].Category.Should().Be("tech_skill");
        report.RequirementGroups[0].Items[0].HandlerCode.Should().Be("H_EXP_D04");
        report.RequirementGroups[0].Items[0].Score.Should().Be(0.75m);
    }

    [Fact]
    public void SerializerToReader_V5_RoundTripsCanonicalGroupTree()
    {
        var item = new ProjectedJdRequirementItem(
            "g1:i1", "tech_skill", "java", "Java 17", "Java", "requirements",
            Array.Empty<string>(), null, null, 1m);
        var group = new ProjectedJdRequirementGroup(
            "g1", "all_of", 1, "must_have", new[] { item }, "requirements",
            "Thành thạo Java", "req-1", "java");
        var projection = new JdRequirementProjection("jd-analysis-effective/v1", new[] { group }, false);
        var assessment = new JdStageTwoItemAssessment(
            item.ItemId, item.Category, "H_TECH_05", 1m, "Đã dùng Java trong dự án A.",
            new[] { new JdMatchingEvidence("Built Java API", "projects") }, Array.Empty<string>());
        var response = new JdStageTwoValidatedResponse(
            new Dictionary<string, JdStageTwoItemAssessment> { [item.ItemId] = assessment },
            "Ứng viên đáp ứng yêu cầu Java.",
            JdStageTwoOutputQuality.COMPLETE,
            new JdStageTwoOutputCoverage(1, 1, 1, 0, Array.Empty<string>(), false),
            Array.Empty<string>());
        var score = new JdFitScoreCalculator().Calculate(projection, response);
        var gaps = new JdCriticalGapEvaluator().Evaluate(projection, response.ItemAssessments);
        var persisted = new JdFitResultSerializer().Serialize(
            projection,
            response,
            score,
            gaps,
            new JdFitSerializationContext(Guid.NewGuid(), "v3.0.0", "semantic", "schema", 1));

        var report = _reader.Read(persisted.JsonString, persisted.FinalScore, "AI");

        report.ReportContract.Should().Be(MatchReportContracts.Version3);
        report.SchemaVersion.Should().Be(JdFitResultContract.Version5);
        report.ScorePercent.Should().Be(100m);
        report.RequirementGroups.Should().ContainSingle();
        report.RequirementGroups[0].SourceRequirementId.Should().Be("req-1");
        report.RequirementGroups[0].SatisfiedItemIds.Should().Equal("g1:i1");
        report.RequirementGroups[0].Items.Should().ContainSingle();
        report.RequirementGroups[0].Items[0].Category.Should().Be("tech_skill");
        report.RequirementGroups[0].Items[0].Evidence.Should().ContainSingle()
            .Which.Section.Should().Be("projects");
    }

    [Fact]
    public void Serializer_V4_AllOfGaps_PersistDistinctStableItemContent()
    {
        var items = new[]
        {
            new ProjectedJdRequirementItem(
                "g1:i1", "tech_skill", "java", "Java 17", "Java", "requirements",
                Array.Empty<string>(), null, null, 1m),
            new ProjectedJdRequirementItem(
                "g1:i2", "experience", "backend experience", "3 years backend", "backend experience", "requirements",
                Array.Empty<string>(), 3, null, 1m)
        };
        var group = new ProjectedJdRequirementGroup(
            "g1", "all_of", 2, "must_have", items, "requirements",
            "Thành thạo Java và có 3 năm kinh nghiệm backend.", "req-1", "backend capability");
        var response = Response(
            Assessment(items[0], 0m, "CV không có bằng chứng sử dụng Java.", "No Java evidence", "skills"),
            Assessment(items[1], 0m, "CV không chứng minh đủ 3 năm backend.", "One student project", "projects"));

        using var document = Serialize(group, response);
        var gaps = document.RootElement.GetProperty("jdFit").GetProperty("criticalGaps").EnumerateArray().ToArray();

        gaps.Should().HaveCount(2);
        gaps.Select(gap => gap.GetProperty("gapId").GetString()).Should().Equal(
            "CRITICAL_GAP:item:g1:g1:i1",
            "CRITICAL_GAP:item:g1:g1:i2");
        gaps.Select(gap => gap.GetProperty("requirement").GetString()).Should().Equal("java", "backend experience");
        gaps.Select(gap => gap.GetProperty("category").GetString()).Should().Equal("tech_skill", "experience");
        gaps.Select(gap => gap.GetProperty("reasoning").GetString()).Should().Equal(
            "CV không có bằng chứng sử dụng Java.",
            "CV không chứng minh đủ 3 năm backend.");
    }

    [Fact]
    public void Serializer_V4_OneOfGap_PersistsJoinedAlternativesAndReasoning()
    {
        var items = new[]
        {
            new ProjectedJdRequirementItem(
                "g1:i1", "tech_skill", "Java", "Java", "Java", "requirements",
                Array.Empty<string>(), null, null, 1m),
            new ProjectedJdRequirementItem(
                "g1:i2", "tech_skill", "C#", "C#", "C#", "requirements",
                Array.Empty<string>(), null, null, 1m)
        };
        var group = new ProjectedJdRequirementGroup(
            "g1", "one_of", 1, "must_have", items, "requirements",
            "Thành thạo Java hoặc C#.", "req-1", "backend language");
        var response = Response(
            Assessment(items[0], 0m, "Không tìm thấy Java."),
            Assessment(items[1], 0m, "Không tìm thấy C#."));

        using var document = Serialize(group, response);
        var gap = document.RootElement.GetProperty("jdFit").GetProperty("criticalGaps")[0];

        gap.GetProperty("gapId").GetString().Should().Be("CRITICAL_GAP:group:g1:g1:i1,g1:i2");
        gap.GetProperty("requirement").GetString().Should().Be("Java | C#");
        gap.GetProperty("requirementVerbatim").GetString().Should().Be("Thành thạo Java hoặc C#.");
        gap.GetProperty("reasoning").GetString().Should().Be("Java: Không tìm thấy Java. C#: Không tìm thấy C#.");
    }

    [Fact]
    public void Serializer_V4_AtLeastNGap_PreservesCountsAndAffectedSourceOrder()
    {
        var items = new[]
        {
            new ProjectedJdRequirementItem("g1:i1", "tech_skill", "Java", "Java", "Java", "requirements", Array.Empty<string>(), null, null, 1m),
            new ProjectedJdRequirementItem("g1:i2", "tech_skill", "C#", "C#", "C#", "requirements", Array.Empty<string>(), null, null, 1m),
            new ProjectedJdRequirementItem("g1:i3", "tech_skill", "Python", "Python", "Python", "requirements", Array.Empty<string>(), null, null, 1m)
        };
        var group = new ProjectedJdRequirementGroup(
            "g1", "at_least_n", 2, "must_have", items, "requirements",
            "Biết ít nhất hai trong Java, C# và Python.", "req-1", "backend languages");
        var response = Response(
            Assessment(items[0], 0.75m, "Có Java."),
            Assessment(items[1], 0m, "Không tìm thấy C#."),
            Assessment(items[2], 0m, "Không tìm thấy Python."));

        using var document = Serialize(group, response);
        var gap = document.RootElement.GetProperty("jdFit").GetProperty("criticalGaps")[0];

        gap.GetProperty("requiredCount").GetInt32().Should().Be(2);
        gap.GetProperty("satisfiedCount").GetInt32().Should().Be(1);
        gap.GetProperty("affectedItemIds").EnumerateArray().Select(value => value.GetString())
            .Should().Equal("g1:i2", "g1:i3");
        gap.GetProperty("gapId").GetString().Should().Be("CRITICAL_GAP:group:g1:g1:i2,g1:i3");
    }

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
                  "satisfiedItemIds":["g1:i1","g1:i2"],
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
                  "operator":"one_of",
                  "requiredCount":1,
                  "satisfiedCount":0,
                  "affectedItemIds":["g1:i1","g1:i2"],
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
        report.ReportContract.Should().Be("match-report/v3");
        report.MatchMethod.Should().Be("one_to_one_ai");
        report.SchemaVersion.Should().Be("jd-matching/v4");
        report.ScorePercent.Should().Be(81.8m);
        report.RequirementGroups.Should().ContainSingle();
        report.RequirementGroups[0].Operator.Should().Be("one_of");
        report.RequirementGroups[0].SatisfiedItemIds.Should().Equal("g1:i1", "g1:i2");
        report.RequirementGroups[0].Items[0].Evidence.Should().ContainSingle()
            .Which.Quotation.Should().Be("Built Java API");
        report.CriticalGaps.Should().ContainSingle();
        report.CriticalGaps[0].Operator.Should().Be("one_of");
        report.CriticalGaps[0].RequiredCount.Should().Be(1);
        report.CriticalGaps[0].SatisfiedCount.Should().Be(0);
        report.CriticalGaps[0].AffectedItemIds.Should().Equal("g1:i1", "g1:i2");
        report.CriticalGaps[0].Evidence.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Quotation = "Student project only",
            Section = "projects"
        });
        report.WarningFlags.Should().Equal("NOTICE");
    }

    [Fact]
    public void Read_V4LegacyItemGap_EnrichesPresentationFromExactGroupItem()
    {
        const string details = """
            {
              "contract":"jd-matching/v4",
              "jdFit":{
                "scorePercent":25,
                "requirementGroups":[{
                  "groupId":"g1","sourceRequirementId":"req-7","operator":"all_of",
                  "importance":"must_have","sourceSection":"requirements",
                  "requirementVerbatim":"Thành thạo Java.","groupScore":0,
                  "items":[{
                    "itemId":"g1:i1","normalizedText":"Java","rawMention":"Java 17",
                    "category":"tech_skill","score":0,"reasoning":"Không có bằng chứng Java.",
                    "evidence":[{"quotation":"Chỉ có JavaScript","section":"skills"}]
                  }]
                }],
                "criticalGaps":[{
                  "code":"CRITICAL_GAP","scope":"item","groupId":"g1","itemId":"g1:i1",
                  "operator":"all_of","requiredCount":1,"satisfiedCount":0,
                  "affectedItemIds":["g1:i1"]
                }]
              }
            }
            """;

        var gap = _reader.Read(details, 25m, "AI").CriticalGaps.Should().ContainSingle().Subject;

        gap.GapId.Should().Be("CRITICAL_GAP:item:g1:g1:i1");
        gap.Requirement.Should().Be("Java");
        gap.Category.Should().Be("tech_skill");
        gap.SourceRequirementId.Should().Be("req-7");
        gap.SourceSection.Should().Be("requirements");
        gap.Importance.Should().Be("must_have");
        gap.RequirementVerbatim.Should().Be("Thành thạo Java.");
        gap.Reasoning.Should().Be("Không có bằng chứng Java.");
        gap.Evidence.Should().ContainSingle().Which.Quotation.Should().Be("Chỉ có JavaScript");
    }

    [Theory]
    [InlineData("one_of", 1, 0)]
    [InlineData("at_least_n", 2, 1)]
    public void Read_V4LegacyGroupGap_EnrichesAlternativesInGroupSourceOrder(
        string operation,
        int requiredCount,
        int satisfiedCount)
    {
        var details = $$"""
            {
              "contract":"jd-matching/v4",
              "jdFit":{
                "scorePercent":25,
                "requirementGroups":[{
                  "groupId":"g1","sourceRequirementId":"req-8","operator":"{{operation}}",
                  "importance":"must_have","sourceSection":"requirements",
                  "requirementVerbatim":"Java hoặc C#.","groupScore":0,
                  "items":[
                    {"itemId":"g1:i1","normalizedText":"Java","category":"tech_skill","score":0,"reasoning":"Thiếu Java.","evidence":[]},
                    {"itemId":"g1:i2","normalizedText":"C#","category":"tech_skill","score":0,"reasoning":"Thiếu C#.","evidence":[]}
                  ]
                }],
                "criticalGaps":[{
                  "code":"CRITICAL_GAP","scope":"group","groupId":"g1","operator":"{{operation}}",
                  "requiredCount":{{requiredCount}},"satisfiedCount":{{satisfiedCount}},
                  "affectedItemIds":["g1:i2","g1:i1"]
                }]
              }
            }
            """;

        var gap = _reader.Read(details, 25m, "AI").CriticalGaps.Should().ContainSingle().Subject;

        gap.GapId.Should().Be("CRITICAL_GAP:group:g1:g1:i1,g1:i2");
        gap.AffectedItemIds.Should().Equal("g1:i1", "g1:i2");
        gap.Requirement.Should().Be("Java | C#");
        gap.Category.Should().Be("tech_skill");
        gap.Reasoning.Should().Be("Java: Thiếu Java. C#: Thiếu C#.");
    }

    [Fact]
    public void Read_V4MalformedGap_DoesNotBorrowAnItemFromAnotherGroup()
    {
        const string details = """
            {
              "contract":"jd-matching/v4",
              "jdFit":{
                "scorePercent":0,
                "requirementGroups":[{
                  "groupId":"real","requirementVerbatim":"Java","items":[{
                    "itemId":"shared","normalizedText":"Java","category":"tech_skill","reasoning":"Missing"
                  }]
                }],
                "criticalGaps":[{
                  "code":"CRITICAL_GAP","scope":"item","groupId":"missing","itemId":"shared",
                  "operator":"all_of","affectedItemIds":["shared"]
                }]
              }
            }
            """;

        var action = () => _reader.Read(details, 0m, "AI");

        var gap = action.Should().NotThrow().Which.CriticalGaps.Should().ContainSingle().Subject;
        gap.Requirement.Should().BeEmpty();
        gap.Category.Should().BeNull();
        gap.Reasoning.Should().BeEmpty();
    }

    [Fact]
    public void Read_V4GapDeduplication_RemovesExactDuplicateButKeepsDifferentItems()
    {
        const string details = """
            {
              "contract":"jd-matching/v4",
              "jdFit":{
                "scorePercent":0,
                "requirementGroups":[{
                  "groupId":"g1","operator":"all_of","importance":"must_have","items":[
                    {"itemId":"a","normalizedText":"Java","category":"tech_skill","reasoning":"Missing Java"},
                    {"itemId":"b","normalizedText":"C#","category":"tech_skill","reasoning":"Missing C#"}
                  ]
                }],
                "criticalGaps":[
                  {"code":"CRITICAL_GAP","scope":"item","groupId":"g1","itemId":"a","operator":"all_of","affectedItemIds":["a"]},
                  {"code":"CRITICAL_GAP","scope":"item","groupId":"g1","itemId":"a","operator":"all_of","affectedItemIds":["a"]},
                  {"code":"CRITICAL_GAP","scope":"item","groupId":"g1","itemId":"b","operator":"all_of","affectedItemIds":["b"]}
                ]
              }
            }
            """;

        var report = _reader.Read(details, 0m, "AI");

        report.CriticalGaps.Should().HaveCount(2);
        report.CriticalGaps.Select(gap => gap.GapId).Should().Equal(
            "CRITICAL_GAP:item:g1:a",
            "CRITICAL_GAP:item:g1:b");
    }

    [Fact]
    public void Read_V4LegacyCoarseGapIds_AreReplacedBeforeDeduplication()
    {
        const string details = """
            {
              "contract":"jd-matching/v4",
              "jdFit":{
                "scorePercent":0,
                "requirementGroups":[{
                  "groupId":"g1","operator":"all_of","importance":"must_have","items":[
                    {"itemId":"a","normalizedText":"Java","category":"tech_skill","reasoning":"Missing Java"},
                    {"itemId":"b","normalizedText":"C#","category":"tech_skill","reasoning":"Missing C#"}
                  ]
                }],
                "criticalGaps":[
                  {"gapId":"CRITICAL_GAP-grp-007","code":"CRITICAL_GAP","scope":"item","groupId":"g1","itemId":"a","operator":"all_of","affectedItemIds":["a"]},
                  {"gapId":"CRITICAL_GAP-grp-007","code":"CRITICAL_GAP","scope":"item","groupId":"g1","itemId":"b","operator":"all_of","affectedItemIds":["b"]}
                ]
              }
            }
            """;

        var report = _reader.Read(details, 0m, "AI");

        report.CriticalGaps.Should().HaveCount(2);
        report.CriticalGaps.Select(gap => gap.GapId).Should().Equal(
            "CRITICAL_GAP:item:g1:a",
            "CRITICAL_GAP:item:g1:b");
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

    [Fact]
    public void Read_V5UnscoredStructuredResult_PreservesNullScoresAndIgnoresStalePersistedScore()
    {
        const string details = """
            {
              "contract":"jd-matching/v5",
              "scoreAvailable":false,
              "completionDisposition":"unscored_refundable",
              "jdFit":{
                "scorePercent":null,
                "narrative":"Recovered assessments are available.",
                "requirementGroups":[{
                  "groupId":"g1",
                  "sourceRequirementId":"req-1",
                  "operator":"all_of",
                  "importance":"must_have",
                  "sourceSection":"requirements",
                  "requirementVerbatim":"Java and SQL",
                  "groupScore":null,
                  "items":[
                    {
                      "itemId":"g1:i1",
                      "normalizedText":"Java",
                      "category":"tech_skill",
                      "assessmentStatus":"assessed",
                      "score":0.75,
                      "handlerCode":"H_TECH_04",
                      "reasoning":"Applied Java experience.",
                      "evidence":[]
                    },
                    {
                      "itemId":"g1:i2",
                      "normalizedText":"SQL",
                      "category":"tech_skill",
                      "assessmentStatus":"unresolved",
                      "score":null,
                      "handlerCode":null,
                      "reasoning":"",
                      "evidence":[]
                    }
                  ]
                }],
                "criticalGaps":[]
              }
            }
            """;

        var report = _reader.Read(details, 91m, "AI");
        decimal? overallScore = report.ScorePercent;
        decimal? groupScore = report.RequirementGroups.Single().GroupScore;
        decimal? unresolvedScore = report.RequirementGroups.Single().Items[1].Score;

        overallScore.Should().BeNull();
        groupScore.Should().BeNull();
        report.RequirementGroups.Single().Items[0].Score.Should().Be(0.75m);
        unresolvedScore.Should().BeNull();
        report.CriticalGaps.Should().BeEmpty();
    }

    [Fact]
    public void Read_RawV2UnscoredResult_PreservesNullAndUsesRawReportKind()
    {
        const string details = """
            {
              "contract":"jd-matching/raw-text-v2",
              "scoreAvailable":false,
              "completionDisposition":"unscored_refundable",
              "resultCode":"SCORE_UNAVAILABLE",
              "jdFit":{
                "score":null,
                "result":null,
                "narrative":"The analysis completed without a reliable score.",
                "requirementGroups":[],
                "criticalGaps":[]
              }
            }
            """;

        var report = _reader.Read(details, 88m, "AI");
        decimal? score = report.ScorePercent;

        report.ReportKind.Should().Be(MatchReportKinds.RawTextFallback);
        report.MatchMethod.Should().Be(MatchMethodCodes.RawTextAi);
        score.Should().BeNull();
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

    private static JsonDocument Serialize(
        ProjectedJdRequirementGroup group,
        JdStageTwoValidatedResponse response)
    {
        var projection = new JdRequirementProjection("jd-analysis-effective/v1", new[] { group }, false);
        var score = new JdFitScoreCalculator().Calculate(projection, response);
        var gaps = new JdCriticalGapEvaluator().Evaluate(projection, response.ItemAssessments);
        var persisted = new JdFitResultSerializer().Serialize(
            projection,
            response,
            score,
            gaps,
            new JdFitSerializationContext(Guid.Parse("11111111-1111-1111-1111-111111111111"), "v3.0.1", "semantic", "schema", 1));
        return JsonDocument.Parse(persisted.JsonString);
    }

    private static JdStageTwoValidatedResponse Response(params JdStageTwoItemAssessment[] assessments) =>
        new(
            assessments.ToDictionary(assessment => assessment.ItemId, StringComparer.Ordinal),
            "Kết quả đánh giá.",
            JdStageTwoOutputQuality.COMPLETE,
            new JdStageTwoOutputCoverage(
                assessments.Length,
                assessments.Length,
                assessments.Length,
                0,
                Array.Empty<string>(),
                false),
            Array.Empty<string>());

    private static JdStageTwoItemAssessment Assessment(
        ProjectedJdRequirementItem item,
        decimal score,
        string reasoning,
        string? quotation = null,
        string? section = null) =>
        new(
            item.ItemId,
            item.Category,
            "H_TEST",
            score,
            reasoning,
            quotation is null
                ? Array.Empty<JdMatchingEvidence>()
                : new[] { new JdMatchingEvidence(quotation, section ?? string.Empty) },
            Array.Empty<string>());
}
