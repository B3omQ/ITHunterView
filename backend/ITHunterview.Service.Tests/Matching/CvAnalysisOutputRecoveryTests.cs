using System.Text.Json;
using FluentAssertions;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public sealed class CvAnalysisOutputRecoveryTests
{
    [Fact]
    public void Recover_ValidDocument_ReturnsCompleteJsonUnchanged()
    {
        const string json = "{\"schema_version\":\"cv-analysis/v2\",\"note\":\"complete\"}";

        var result = CvAnalysisOutputRecovery.Recover(json);

        result.Mode.Should().Be(CvAnalysisRecoveryMode.COMPLETE_JSON);
        result.WasTruncated.Should().BeFalse();
        result.Json.Should().Be(json);
    }

    [Fact]
    public void Recover_CompleteObjectWrappedInProviderProse_ExtractsObject()
    {
        const string json = "{\"schema_version\":\"cv-analysis/v2\",\"note\":\"complete\"}";

        var result = CvAnalysisOutputRecovery.Recover($"I extracted the CV below.\n{json}\nEnd of response.");

        result.Mode.Should().Be(CvAnalysisRecoveryMode.EXTRACTED_COMPLETE_OBJECT);
        result.Json.Should().Be(json);
        result.WasTruncated.Should().BeFalse();
    }

    [Fact]
    public void Recover_TrailingComma_ReserializesStrictJsonWithoutChangingValues()
    {
        var result = CvAnalysisOutputRecovery.Recover(
            "{\"schema_version\":\"cv-analysis/v2\",\"skills\":[\"C#\",],}");

        result.Mode.Should().Be(CvAnalysisRecoveryMode.NORMALIZED_JSON);
        using var document = JsonDocument.Parse(result.Json!);
        document.RootElement.GetProperty("schema_version").GetString().Should().Be("cv-analysis/v2");
        document.RootElement.GetProperty("skills")[0].GetString().Should().Be("C#");
    }

    [Fact]
    public void Recover_BracesInsideQuotedStrings_DoesNotEndObjectEarly()
    {
        const string json = "{\"schema_version\":\"cv-analysis/v2\",\"note\":\"Implemented {feature} and escaped \\\"quotes\\\"\"}";

        var result = CvAnalysisOutputRecovery.Recover($"prefix {json} suffix");

        result.Mode.Should().Be(CvAnalysisRecoveryMode.EXTRACTED_COMPLETE_OBJECT);
        using var document = JsonDocument.Parse(result.Json!);
        document.RootElement.GetProperty("note").GetString()
            .Should().Be("Implemented {feature} and escaped \"quotes\"");
    }

    [Fact]
    public void Recover_TruncatedAfterTwoCompleteExperiences_KeepsOnlyClosedEntries()
    {
        var result = CvAnalysisOutputRecovery.Recover(TruncatedAfterTwoExperiences());

        result.Mode.Should().Be(CvAnalysisRecoveryMode.RECOVERED_PARTIAL,
            $"diagnostics={string.Join(',', result.Diagnostics.Select(x => x.Code + ':' + x.JsonPath))}; json={result.Json}");
        result.WasTruncated.Should().BeTrue();
        result.Diagnostics.Should().Contain(x => x.Code == "OUTPUT_TRUNCATED");
        result.Diagnostics.Should().Contain(x => x.Code == "RECOVERED_COMPLETE_CV_CONTENT");
        using var recovered = JsonDocument.Parse(result.Json!);
        recovered.RootElement.GetProperty("schema_version").GetString().Should().Be("cv-analysis/v2");
        recovered.RootElement.TryGetProperty("analysis_quality", out _).Should().BeFalse();
        recovered.RootElement.GetProperty("verbatim_sections")
            .GetProperty("professional_experience_and_projects").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public void Recover_TruncatedInsideFirstExperience_ReturnsInvalidWhenNoUsableUnitExists()
    {
        var result = CvAnalysisOutputRecovery.Recover(TruncatedInsideFirstExperience());

        result.Mode.Should().Be(CvAnalysisRecoveryMode.INVALID);
        result.Json.Should().BeNull();
    }

    [Fact]
    public void Recover_TruncatedAfterCompleteMetrics_PreservesExactMetricValues()
    {
        var result = CvAnalysisOutputRecovery.Recover(TruncatedAfterCompleteMetrics());

        result.Mode.Should().Be(CvAnalysisRecoveryMode.RECOVERED_PARTIAL);
        using var recovered = JsonDocument.Parse(result.Json!);
        var metrics = recovered.RootElement.GetProperty("matching_metrics");
        metrics.GetProperty("total_years_exp").GetInt32().Should().Be(7);
        metrics.GetProperty("job_titles_normalized")[0].GetString().Should().Be("Principal Engineer");
    }

    [Fact]
    public void Recover_TruncatedVietnameseUtf8_DoesNotCorruptCompletedText()
    {
        var result = CvAnalysisOutputRecovery.Recover(TruncatedVietnameseText());

        result.Mode.Should().Be(CvAnalysisRecoveryMode.RECOVERED_PARTIAL);
        using var recovered = JsonDocument.Parse(result.Json!);
        recovered.RootElement.GetProperty("verbatim_sections").GetProperty("personal_info")
            .GetProperty("summary").GetString().Should().Be("Kỹ sư phần mềm phát triển hệ thống phân tán.");
    }

    [Fact]
    public void Recover_UnsupportedObservedSchema_ReturnsInvalid()
    {
        var result = CvAnalysisOutputRecovery.Recover(
            "{\"schema_version\":\"cv-analysis/v3\",\"verbatim_sections\":{\"personal_info\":{\"summary\":\"text\"}}}");

        result.Mode.Should().Be(CvAnalysisRecoveryMode.INVALID);
        result.Json.Should().BeNull();
    }

    [Fact]
    public void Recover_TruncatedEnvelope_IsAcceptedAsUsablePartialByRealValidator()
    {
        var recovered = CvAnalysisOutputRecovery.Recover(TruncatedAfterTwoExperiences());
        var validation = new CvAnalysisResponseValidator().ValidateRecovered(recovered);

        validation.IsUsable.Should().BeTrue();
        validation.Quality.Should().Be(CvAnalysisQuality.PARTIAL);
        validation.Diagnostics.Should().Contain(x => x.Code == "OUTPUT_TRUNCATED");
    }

    [Fact]
    public void Recover_TruncatedBeforeCalculationBasis_DoesNotInventCalculationBasis()
    {
        var result = CvAnalysisOutputRecovery.Recover(TruncatedBeforeCalculationBasis());

        result.Mode.Should().Be(CvAnalysisRecoveryMode.RECOVERED_PARTIAL);
        using var recovered = JsonDocument.Parse(result.Json!);
        var summary = recovered.RootElement.GetProperty("matching_evidence").GetProperty("experience_summary");
        summary.TryGetProperty("calculation_basis", out _).Should().BeFalse();
    }

    [Fact]
    public void Recover_MoreThanFormerCap_RetainsEveryCompletedUnit()
    {
        var result = CvAnalysisOutputRecovery.Recover(TruncatedAfterCompletedExperiences(35));

        result.Mode.Should().Be(CvAnalysisRecoveryMode.RECOVERED_PARTIAL);
        using var recovered = JsonDocument.Parse(result.Json!);
        recovered.RootElement.GetProperty("verbatim_sections")
            .GetProperty("professional_experience_and_projects").GetArrayLength().Should().Be(35);
    }

    [Fact]
    public void Recover_AcceptedCountsEqualSerializedUnits()
    {
        var result = CvAnalysisOutputRecovery.Recover(TruncatedAfterCompletedExperiences(35));

        using var recovered = JsonDocument.Parse(result.Json!);
        var serializedCount = recovered.RootElement.GetProperty("verbatim_sections")
            .GetProperty("professional_experience_and_projects").GetArrayLength();
        result.Coverage!.AcceptedExperienceEntryCount.Should().Be(serializedCount);
        result.Coverage.InputExperienceEntryCount.Should().Be(36);
        result.Coverage.DiscardedExperienceEntryCount.Should().Be(1);
    }

    private static string TruncatedAfterTwoExperiences() => """
        {
          "schema_version": "cv-analysis/v2",
          "verbatim_sections": {
            "personal_info": {"name":"Nguyễn Văn A","title":"Backend Engineer","summary":"Experienced engineer."},
            "education": [], "languages": [], "skills_section": ["C#"],
            "professional_experience_and_projects": [
              {"company_or_project_name":"Alpha","role":"Engineer","timeline":"2020-2022","entry_type":"work","details_and_responsibilities":["Built APIs"],"technologies_used":["C#"]},
              {"company_or_project_name":"Beta","role":"Senior Engineer","timeline":"2022-2024","entry_type":"work","details_and_responsibilities":["Led delivery"],"technologies_used":[".NET"]},
              {"company_or_project_name":"Gamma","role":"Lead Engineer","timeline":"2024-","entry_type":"work","details_and_responsibilities":["Still working
        """;

    private static string TruncatedInsideFirstExperience() => """
        {
          "schema_version": "cv-analysis/v2",
          "verbatim_sections": {
            "personal_info": {"name":"A","title":"","summary":""},
            "education": [], "languages": [], "skills_section": [],
            "professional_experience_and_projects": [
              {"company_or_project_name":"Alpha","role":"Engineer"
        """;

    private static string TruncatedAfterCompleteMetrics() => """
        {
          "schema_version": "cv-analysis/v2",
          "verbatim_sections": {"personal_info":{"name":"A","title":"Principal Engineer","summary":""},"education":[],"languages":[],"skills_section":[],"professional_experience_and_projects":[],"certifications_and_awards":[],"other_information":""},
          "matching_metrics": {"job_titles_normalized":["Principal Engineer"],"skills_normalized":["C#"],"total_years_exp":7,"domains":["backend"]},
          "matching_evidence": {"requirement_signals":[],"experience_summary":{"total_professional_months":84,"calculation_basis":"CV","periods":[]},"seniority_signals":[]
        """;

    private static string TruncatedVietnameseText() => """
        {
          "schema_version": "cv-analysis/v2",
          "verbatim_sections": {
            "personal_info": {"name":"Nguyễn Văn A","title":"Kỹ sư phần mềm","summary":"Kỹ sư phần mềm phát triển hệ thống phân tán."},
            "education": [], "languages": [], "skills_section": [], "professional_experience_and_projects": [], "certifications_and_awards": [], "other_information":""
          },
          "matching_metrics": {"job_titles_normalized":[],"skills_normalized":[],"total_years_exp":0,"domains":[]},
          "matching_evidence": {"requirement_signals":[],"experience_summary":{"total_professional_months":0,"calculation_basis":"","periods":[]},"seniority_signals":[]
        """;

    private static string TruncatedBeforeCalculationBasis() => """
        {
          "schema_version": "cv-analysis/v2",
          "verbatim_sections": {"personal_info":{"name":"A","title":"Engineer","summary":""},"education":[],"languages":[],"skills_section":["C#"],"professional_experience_and_projects":[],"certifications_and_awards":[],"other_information":""},
          "matching_metrics": {"job_titles_normalized":["Engineer"],"skills_normalized":["C#"],"total_years_exp":3,"domains":[]},
          "matching_evidence": {"requirement_signals":[],"experience_summary":{"total_professional_months":36,"calculation_basis":"unfinished
        """;

    private static string TruncatedAfterCompletedExperiences(int completedCount)
    {
        var entries = string.Join(",", Enumerable.Range(0, completedCount).Select(index =>
            $$"""{"company_or_project_name":"Project {{index}}","role":"Engineer","timeline":"2020-2021","entry_type":"project","details_and_responsibilities":["Delivered {{index}}"],"technologies_used":["C#"]}"""));
        return $$"""
            {
              "schema_version":"cv-analysis/v2",
              "verbatim_sections":{
                "personal_info":{"name":"A","title":"Engineer","summary":""},
                "education":[],"languages":[],"skills_section":["C#"],
                "professional_experience_and_projects":[{{entries}},{"company_or_project_name":"unfinished
            """;
    }
}
