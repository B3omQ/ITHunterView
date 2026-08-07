namespace ITHunterview.Service.Interface.Service;

/// <summary>Per-call generation settings. Defaults remain provider-owned.</summary>
public sealed record AiGenerationOptions(
    decimal? Temperature,
    decimal? TopP,
    int? MaxOutputTokens,
    string? ResponseMimeType,
    string ProfileId,
    int? MaxTransportAttempts = null)
{
    public static readonly AiGenerationOptions StrictJsonExtraction = new(
        Temperature: 0m,
        TopP: 0.1m,
        MaxOutputTokens: 8192,
        ResponseMimeType: "application/json",
        ProfileId: "jd-analysis-json/v1",
        MaxTransportAttempts: 1);

    public static readonly AiGenerationOptions CvAnalysisJsonExtraction = new(
        Temperature: 0m,
        TopP: 0.1m,
        MaxOutputTokens: 8192,
        ResponseMimeType: "application/json",
        ProfileId: "cv-analysis-json/v1");
}
