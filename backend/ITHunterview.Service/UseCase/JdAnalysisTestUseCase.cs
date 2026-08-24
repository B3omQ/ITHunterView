using System.Text.Json;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Service;
using ITHunterview.Service.Utils;

namespace ITHunterview.Service.UseCase;

public sealed class JdAnalysisTestUseCase : IJdAnalysisTestUseCase
{
    private readonly IJobAnalysisInputBuilder _inputBuilder;
    private readonly IJobAnalysisExtractionService _extractionService;

    public JdAnalysisTestUseCase(
        IJobAnalysisInputBuilder inputBuilder,
        IJobAnalysisExtractionService extractionService)
    {
        _inputBuilder = inputBuilder;
        _extractionService = extractionService;
    }

    public async Task<JsonElement> AnalyzeAsync(string jdText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(jdText))
        {
            throw new ArgumentException("JD text is required.", nameof(jdText));
        }

        var input = _inputBuilder.BuildFromPastedText(null, jdText);
        var extraction = await _extractionService.ExtractWithActivePromptsAsync(input, ct);
        if (extraction.Quality == JdAnalysisQuality.INVALID || extraction.Validation.Data is null)
        {
            var failureCode = extraction.Validation.FailureCode
                ?? extraction.Diagnostics.FirstOrDefault()?.Code
                ?? "INVALID_MODEL_OUTPUT";
            throw new JobAnalysisException(
                "JD_ANALYSIS_INVALID",
                422,
                $"JD analysis did not produce valid structured JSON. Failure code: {failureCode}.");
        }

        var canonicalJson = _extractionService.SerializeEffectiveAnalysis(extraction.Validation.Data);
        using var document = JsonDocument.Parse(canonicalJson);
        return document.RootElement.Clone();
    }
}
