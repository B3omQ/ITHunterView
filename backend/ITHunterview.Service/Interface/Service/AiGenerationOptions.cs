namespace ITHunterview.Service.Interface.Service;

/// <summary>Per-call generation settings. Defaults remain provider-owned.</summary>
public sealed record AiGenerationOptions(
    decimal? Temperature,
    decimal? TopP,
    int? MaxOutputTokens,
    string? ResponseMimeType,
    string ProfileId,
    int? MaxTransportAttempts = null,
    int? ThinkingBudget = null,
    string? ThinkingLevel = null)
{
    public static readonly AiGenerationOptions StrictJsonExtraction = new(
        Temperature: 0m,
        TopP: 0.1m,
        MaxOutputTokens: 16384,
        ResponseMimeType: "application/json",
        ProfileId: "jd-analysis-json/v1",
        MaxTransportAttempts: 1,
        ThinkingBudget: 3000,
        ThinkingLevel: "medium");

    public static readonly AiGenerationOptions CvAnalysisJsonExtraction = new(
        Temperature: 0m,
        TopP: 0.1m,
        MaxOutputTokens: 8192,
        ResponseMimeType: "application/json",
        ProfileId: "cv-analysis-json/v1",
        MaxTransportAttempts: 1,
        ThinkingBudget: 1000,
        ThinkingLevel: "minimal");

    public static readonly AiGenerationOptions CvAnalysisJsonRetry = new(
        Temperature: 0m,
        TopP: 0.1m,
        MaxOutputTokens: 12288,
        ResponseMimeType: "application/json",
        ProfileId: "cv-analysis-json-retry/v1",
        MaxTransportAttempts: 1,
        ThinkingBudget: 1000,
        ThinkingLevel: "minimal");

    public static readonly AiGenerationOptions JdMatchingJsonScoring = new(
        Temperature: 0.2m,
        TopP: 0.1m,
        MaxOutputTokens: 16384,
        ResponseMimeType: "application/json",
        ProfileId: "jd-matching-json/v1",
        MaxTransportAttempts: 1,
        ThinkingBudget: 3000,
        ThinkingLevel: "medium");

    public static readonly AiGenerationOptions JdMatchingJsonRetry = new(
        Temperature: 0.2m,
        TopP: 0.1m,
        MaxOutputTokens: 20480,
        ResponseMimeType: "application/json",
        ProfileId: "jd-matching-json-retry/v1",
        MaxTransportAttempts: 1,
        ThinkingBudget: 3000,
        ThinkingLevel: "medium");
}
