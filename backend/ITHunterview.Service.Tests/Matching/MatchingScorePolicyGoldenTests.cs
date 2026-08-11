using System.Reflection;
using System.Text.Json;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public sealed class MatchingScorePolicyGoldenTests
{
    private const string ExpectedWorkbookHash = "e06fa8131c1e6a9b92caedb1a80887a39296dc68329fdf8194f63373397fb148";

    [Fact]
    public void GoldenFixture_PreservesTheReviewedWorkbookPolicy()
    {
        var fixture = ReadFixture();

        Assert.Equal("matching-score-policy/v1", fixture.PolicyVersion);
        Assert.Equal(ExpectedWorkbookHash, fixture.SourceWorkbook.Sha256);
        Assert.Equal(
            new[] { "domain_knowledge", "education", "experience", "language", "soft_skill", "tech_skill" },
            fixture.Categories.Select(category => category.Code).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(fixture.Categories, category => category.Code == "seniority_fit");
        Assert.Equal(44, fixture.Handlers.Count);
        Assert.Equal(41, fixture.Handlers.Count(handler => handler.Score.HasValue));
        Assert.Equal(3, fixture.Handlers.Count(handler => !handler.Score.HasValue));
        Assert.Equal(
            new[] { 0m, 0.25m, 0.5m, 0.75m, 1m },
            fixture.Handlers.Where(handler => handler.Score.HasValue)
                .Select(handler => handler.Score!.Value)
                .Distinct()
                .Order());
        Assert.Equal(fixture.Handlers.Count, fixture.Handlers.Select(handler => handler.HandlerCode).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(5, fixture.ResultBands.Count);
        Assert.Equal(0m, fixture.ResultBands.Min(band => band.LowerInclusive));
        Assert.Equal(100m, fixture.ResultBands.Max(band => band.UpperInclusive));
    }

    [Fact]
    public void ProductionPolicy_MapsEveryGoldenHandlerWeightAndBandWithoutExtras()
    {
        var fixture = ReadFixture();
        var policyType = typeof(MatchingHandlerCodePolicy).Assembly.GetType(
            "ITHunterview.Service.Service.Matching.MatchingScorePolicy");

        Assert.NotNull(policyType);
        var tryResolve = RequiredMethod(policyType!, "TryResolveHandler");
        var categoryWeight = RequiredMethod(policyType!, "GetCategoryWeight");
        var importanceMultiplier = RequiredMethod(policyType!, "GetImportanceMultiplier");
        var resolveBand = RequiredMethod(policyType!, "ResolveBand");

        foreach (var expected in fixture.Handlers)
        {
            object?[] arguments = [expected.Category, expected.HandlerCode, null];
            var accepted = Assert.IsType<bool>(tryResolve.Invoke(null, arguments));
            if (!expected.Score.HasValue)
            {
                Assert.False(accepted);
                continue;
            }

            Assert.True(accepted);
            Assert.NotNull(arguments[2]);
            Assert.Equal(expected.Category, ReadProperty<string>(arguments[2]!, "Category"));
            Assert.Equal(expected.HandlerCode, ReadProperty<string>(arguments[2]!, "HandlerCode"));
            Assert.Equal(expected.MatchLevel, ReadProperty<string>(arguments[2]!, "MatchLevel"));
            Assert.Equal(expected.Score.Value, ReadProperty<decimal>(arguments[2]!, "Score"));
            Assert.Equal(expected.OutputStatus, ReadProperty<string>(arguments[2]!, "OutputStatus"));
        }

        foreach (var expected in fixture.Categories)
        {
            Assert.Equal(expected.Weight, Assert.IsType<decimal>(categoryWeight.Invoke(null, [expected.Code])));
        }

        foreach (var expected in fixture.Importance.Where(value => value.Scorable))
        {
            Assert.Equal(expected.Multiplier, Assert.IsType<decimal>(importanceMultiplier.Invoke(null, [expected.Code])));
        }

        foreach (var expected in fixture.ResultBands)
        {
            var atLower = resolveBand.Invoke(null, [expected.LowerInclusive]);
            var atUpper = resolveBand.Invoke(null, [expected.UpperInclusive]);
            Assert.NotNull(atLower);
            Assert.NotNull(atUpper);
            Assert.Equal(expected.ResultCode, ReadProperty<string>(atLower!, "ResultCode"));
            Assert.Equal(expected.ResultCode, ReadProperty<string>(atUpper!, "ResultCode"));
        }
    }

    [Fact]
    public void HandlerAllowlist_UsesScorableGoldenCodesOnly()
    {
        Assert.True(MatchingHandlerCodePolicy.IsValid("experience", "H_EXP_D05"));
        Assert.True(MatchingHandlerCodePolicy.IsValid("language", "H_LANG_F04"));
        Assert.False(MatchingHandlerCodePolicy.IsValid("experience", "H_EXP_00"));
        Assert.False(MatchingHandlerCodePolicy.IsValid("seniority_fit", "H_SENIOR_05"));
        Assert.False(MatchingHandlerCodePolicy.IsKnown("H_SENIOR_05"));
    }

    [Theory]
    [InlineData("H_EXP_D01")]
    [InlineData("h_exp_d01")]
    [InlineData("  H_EXP_D01  ")]
    public void TryResolveHandlerCode_KnownScoringCode_ReturnsCanonicalResolution(string input)
    {
        var resolved = MatchingScorePolicy.TryResolveHandlerCode(input, out var resolution);

        Assert.True(resolved);
        Assert.Equal("experience", resolution.Category);
        Assert.Equal("H_EXP_D01", resolution.HandlerCode);
        Assert.Equal(0m, resolution.Score);
        Assert.Equal("NOT_EVIDENCED", resolution.OutputStatus);
    }

    [Theory]
    [InlineData("H_EXP_00")]
    [InlineData("H_EDU_00")]
    [InlineData("H_LANG_00")]
    public void TryResolveHandlerCode_NonScoringCode_ReturnsFalse(string input)
    {
        Assert.False(MatchingScorePolicy.TryResolveHandlerCode(input, out _));
        Assert.True(MatchingHandlerCodePolicy.IsNonScoringCode(input));
        Assert.True(MatchingHandlerCodePolicy.IsKnown(input));
    }

    [Fact]
    public void TryResolveHandlerCode_UnknownCode_ReturnsFalse()
    {
        Assert.False(MatchingScorePolicy.TryResolveHandlerCode("H_UNKNOWN_99", out _));
        Assert.False(MatchingHandlerCodePolicy.IsNonScoringCode("H_UNKNOWN_99"));
        Assert.False(MatchingHandlerCodePolicy.IsKnown("H_UNKNOWN_99"));
    }

    [Fact]
    public void GlobalScoringLookup_HasSameCountAsScoreBearingGoldenHandlers()
    {
        var fixture = ReadFixture();
        var expected = fixture.Handlers.Where(handler => handler.Score.HasValue).ToArray();

        foreach (var handler in expected)
        {
            Assert.True(MatchingScorePolicy.TryResolveHandlerCode(handler.HandlerCode, out var resolution));
            Assert.Equal(handler.HandlerCode, resolution.HandlerCode);
            Assert.Equal(handler.Category, resolution.Category);
            Assert.Equal(handler.Score, resolution.Score);
            Assert.Equal(handler.OutputStatus, resolution.OutputStatus);
        }

        Assert.Equal(
            expected.Length,
            expected.Select(handler => handler.HandlerCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());
    }

    private static MethodInfo RequiredMethod(Type type, string name) =>
        type.GetMethod(name, BindingFlags.Public | BindingFlags.Static)
        ?? throw new Xunit.Sdk.XunitException($"Missing public static method {type.FullName}.{name}.");

    private static T ReadProperty<T>(object instance, string name) =>
        Assert.IsType<T>(instance.GetType().GetProperty(name)?.GetValue(instance));

    private static PolicyFixture ReadFixture()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Matching", "Fixtures", "matching-score-policy-v1.json");
        return JsonSerializer.Deserialize<PolicyFixture>(File.ReadAllText(path), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("MATCHING_SCORE_POLICY_FIXTURE_INVALID");
    }

    private sealed record PolicyFixture(
        string PolicyVersion,
        SourceWorkbookFixture SourceWorkbook,
        IReadOnlyList<ImportanceFixture> Importance,
        IReadOnlyList<CategoryFixture> Categories,
        IReadOnlyList<HandlerFixture> Handlers,
        IReadOnlyList<ResultBandFixture> ResultBands);

    private sealed record SourceWorkbookFixture(string Name, string Sha256);
    private sealed record ImportanceFixture(string Code, decimal Multiplier, bool Scorable);
    private sealed record CategoryFixture(string Code, decimal Weight);
    private sealed record HandlerFixture(
        string Category,
        string HandlerCode,
        string MatchLevel,
        decimal? Score,
        string OutputStatus);
    private sealed record ResultBandFixture(
        string ResultCode,
        decimal LowerInclusive,
        decimal UpperInclusive);
}
