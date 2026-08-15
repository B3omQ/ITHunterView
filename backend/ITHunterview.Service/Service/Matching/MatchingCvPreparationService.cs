using ITHunterview.Domain.Entities;
using ITHunterview.Service.Config;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Exceptions;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Utils;
using Microsoft.Extensions.Options;

namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Turns an immutable matching CV snapshot into usable canonical analysis.
/// It never writes the source entity; a successful reparse is carried to the
/// durable worker as an intent and is persisted only after match completion.
/// </summary>
public sealed class MatchingCvPreparationService : IMatchingCvPreparationService
{
    private readonly ICvAnalysisResponseValidator _validator;
    private readonly ICvTextExtractorService _extractor;
    private readonly IMatchingSourceRepository _sources;
    private readonly string _cloudName;

    public MatchingCvPreparationService(
        ICvAnalysisResponseValidator validator,
        ICvTextExtractorService extractor,
        IMatchingSourceRepository sources,
        IOptions<CloudinarySettings> cloudinarySettings)
    {
        _validator = validator;
        _extractor = extractor;
        _sources = sources;
        _cloudName = cloudinarySettings.Value.CloudName?.Trim() ?? string.Empty;
    }

    public async Task<PreparedCvMatchingInput> PrepareAsync(
        Guid userId,
        MatchingInputSnapshotV1 snapshot,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var cv = snapshot.Cv ?? throw new InvalidOperationException("MATCHING_CV_PREPARATION_INVALID");

        var stored = string.Equals(cv.SourceParseStatus, "SUCCESS", StringComparison.Ordinal)
            ? ValidateStored(cv.AnalysisJson)
            : CvAnalysisValidationResult.Invalid(
                "CV_ANALYSIS_EMPTY_OUTPUT",
                "STORED_ANALYSIS_NOT_READY",
                "$");
        if (stored.IsUsable)
        {
            return ToPrepared(stored, null);
        }

        var source = await ResolveSourceAsync(userId, snapshot, ct);
        var parsed = source.FileUrl is not null
            ? await _extractor.ExtractParsedDataFromUrlAsync(source.FileUrl, source.RawText, ct)
            : await _extractor.ExtractParsedDataFromRawTextAsync(source.RawText, "pasted_text", source.FileName, ct);
        var extracted = ValidateStored(parsed);
        if (!extracted.IsUsable)
        {
            throw new CvAnalysisValidationException(extracted);
        }

        return ToPrepared(extracted, source.PersistenceIntent);
    }

    private CvAnalysisValidationResult ValidateStored(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? CvAnalysisValidationResult.Invalid("CV_ANALYSIS_EMPTY_OUTPUT", "EMPTY_STORED_ANALYSIS", "$")
            : _validator is ICvAnalysisRecoveryAwareValidator recoveryAware
                ? recoveryAware.ValidateStoredCanonical(json)
                : _validator.ValidateAndCanonicalize(json);

    private async Task<ResolvedCvSource> ResolveSourceAsync(
        Guid userId,
        MatchingInputSnapshotV1 snapshot,
        CancellationToken ct)
    {
        var cv = snapshot.Cv;
        var isSaved = string.Equals(cv.SourceKind, "saved_cv", StringComparison.Ordinal);
        if (!isSaved)
        {
            return RequireRawSource(cv.OriginalText, cv.FileName);
        }

        var fileUrl = TryNormalizeCloudinaryUrl(cv.FileUrl);
        var rawText = cv.OriginalText;
        Guid? sourceId = cv.SourceId;
        string? expectedSourceHash = cv.SourceContentHash;
        string? expectedAnalysisHash = cv.SourceContentHash is null ? null : MatchingSourceFingerprint.ForAnalysis(cv.AnalysisJson);

        if (snapshot.SchemaVersion == MatchingInputSnapshotBuilder.LegacySchemaVersion &&
            fileUrl is null && string.IsNullOrWhiteSpace(rawText) && sourceId.HasValue)
        {
            var live = await _sources.GetOwnedCvAsync(sourceId.Value, userId, ct)
                ?? throw new InvalidOperationException("MATCHING_CV_SOURCE_UNAVAILABLE");
            fileUrl = TryNormalizeCloudinaryUrl(live.FileUrl);
            rawText = live.RawText ?? string.Empty;
            expectedSourceHash = MatchingSourceFingerprint.ForCv(fileUrl, rawText);
            expectedAnalysisHash = MatchingSourceFingerprint.ForAnalysis(live.ParsedData);
        }

        if (fileUrl is null && string.IsNullOrWhiteSpace(rawText))
        {
            throw new InvalidOperationException("MATCHING_CV_SOURCE_UNAVAILABLE");
        }

        CvAnalysisPersistenceIntent? intent = null;
        if (sourceId.HasValue && !string.IsNullOrWhiteSpace(expectedSourceHash) && !string.IsNullOrWhiteSpace(expectedAnalysisHash))
        {
            intent = new CvAnalysisPersistenceIntent(
                sourceId.Value,
                userId,
                expectedSourceHash,
                expectedAnalysisHash,
                string.Empty,
                default,
                null,
                null);
        }

        return new ResolvedCvSource(fileUrl, rawText, cv.FileName, intent);
    }

    private static ResolvedCvSource RequireRawSource(string rawText, string? fileName)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            throw new InvalidOperationException("MATCHING_CV_SOURCE_UNAVAILABLE");
        }
        return new ResolvedCvSource(null, rawText, fileName, null);
    }

    private PreparedCvMatchingInput ToPrepared(CvAnalysisValidationResult validation, CvAnalysisPersistenceIntent? intent)
    {
        if (intent is not null)
        {
            intent = intent with
            {
                CanonicalJson = validation.CanonicalJson,
                Quality = validation.Quality,
                CoverageJson = CvAnalysisMetadataReader.SerializeCoverage(validation.Coverage),
                DiagnosticsJson = CvAnalysisMetadataReader.SerializeDiagnostics(validation.Diagnostics)
            };
        }

        return new PreparedCvMatchingInput(
            validation.CanonicalJson,
            validation.Quality,
            validation.Coverage,
            validation.Diagnostics,
            intent);
    }

    private string? TryNormalizeCloudinaryUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 2_048 ||
            !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.Host, "res.cloudinary.com", StringComparison.OrdinalIgnoreCase) ||
            uri.UserInfo.Length > 0 || uri.Fragment.Length > 0 || uri.Port != 443 ||
            string.IsNullOrWhiteSpace(_cloudName))
        {
            return null;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 0 && string.Equals(segments[0], _cloudName, StringComparison.Ordinal)
            ? uri.AbsoluteUri
            : null;
    }

    private sealed record ResolvedCvSource(
        string? FileUrl,
        string RawText,
        string? FileName,
        CvAnalysisPersistenceIntent? PersistenceIntent);
}
