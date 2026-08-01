using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;

namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Validates only the public request shape. It deliberately has no database
/// dependency, so authorization is always a separate preflight concern.
/// </summary>
public sealed class MatchingRequestValidator : IMatchingRequestValidator
{
    public const int MinimumRawTextLength = 100;
    public const int MaximumRawTextLength = 100_000;
    public const int MaximumMetadataLength = 255;

    public MatchingRequestValidationResult Validate(MatchingRequestDto request)
    {
        if (request is null)
        {
            return MatchingRequestValidationResult.Failure("CV_SOURCE_REQUIRED");
        }

        if (request.Mode != MatchingMode.JdFit)
        {
            return MatchingRequestValidationResult.Failure("MATCHING_MODE_NOT_SUPPORTED");
        }

        if (!string.IsNullOrWhiteSpace(request.CvUrl))
        {
            return MatchingRequestValidationResult.Failure("CV_URL_SOURCE_NOT_SUPPORTED");
        }

        var hasCvId = request.CvId.HasValue;
        var hasCvText = !string.IsNullOrWhiteSpace(request.CvText);
        var cvSourceCount = (hasCvId ? 1 : 0) + (hasCvText ? 1 : 0);
        if (cvSourceCount == 0)
        {
            return MatchingRequestValidationResult.Failure("CV_SOURCE_REQUIRED");
        }

        if (cvSourceCount > 1)
        {
            return MatchingRequestValidationResult.Failure("MULTIPLE_CV_SOURCES");
        }

        var hasJobId = request.JobId.HasValue;
        var hasRawJdText = !string.IsNullOrWhiteSpace(request.RawJdText);
        var jdSourceCount = (hasJobId ? 1 : 0) + (hasRawJdText ? 1 : 0);
        if (jdSourceCount == 0)
        {
            return MatchingRequestValidationResult.Failure("JD_SOURCE_REQUIRED");
        }

        if (jdSourceCount > 1)
        {
            return MatchingRequestValidationResult.Failure("MULTIPLE_JD_SOURCES");
        }

        if (request.CvId == Guid.Empty)
        {
            return MatchingRequestValidationResult.Failure("CV_ID_INVALID");
        }

        if (request.JobId == Guid.Empty)
        {
            return MatchingRequestValidationResult.Failure("JOB_ID_INVALID");
        }

        var cvText = request.CvText?.Trim();
        if (hasCvText && cvText!.Length < MinimumRawTextLength)
        {
            return MatchingRequestValidationResult.Failure("CV_TEXT_TOO_SHORT");
        }

        if (hasCvText && cvText!.Length > MaximumRawTextLength)
        {
            return MatchingRequestValidationResult.Failure("CV_TEXT_TOO_LONG");
        }

        var rawJdText = request.RawJdText?.Trim();
        if (hasRawJdText && rawJdText!.Length < MinimumRawTextLength)
        {
            return MatchingRequestValidationResult.Failure("JD_TEXT_TOO_SHORT");
        }

        if (hasRawJdText && rawJdText!.Length > MaximumRawTextLength)
        {
            return MatchingRequestValidationResult.Failure("JD_TEXT_TOO_LONG");
        }

        var cvFileName = request.CvFileName?.Trim();
        if (cvFileName?.Length > MaximumMetadataLength)
        {
            return MatchingRequestValidationResult.Failure("CV_FILE_NAME_TOO_LONG");
        }

        var jdTitle = request.JdTitle?.Trim();
        if (jdTitle?.Length > MaximumMetadataLength)
        {
            return MatchingRequestValidationResult.Failure("JD_TITLE_TOO_LONG");
        }

        return MatchingRequestValidationResult.Success(new MatchingInputSelection(
            request.CvId,
            cvText,
            request.JobId,
            rawJdText,
            cvFileName,
            jdTitle,
            request.Mode));
    }
}
