namespace ITHunterview.Service.DTOs.Cv.Matching;

/// <summary>
/// A request that has passed structural validation. Database authorization is
/// performed by <c>IMatchingInputPreflightUseCase</c> before it is promoted to
/// <see cref="PreparedMatchingRequest"/>.
/// </summary>
public sealed record MatchingInputSelection(
    Guid? CvId,
    string? CvText,
    Guid? JobId,
    string? RawJdText,
    string? CvFileName,
    string? JdTitle,
    MatchingMode Mode);

public abstract record PreparedCvSource;

public sealed record PreparedSavedCvSource(Guid CvId, string FileName) : PreparedCvSource;

public sealed record PreparedRawCvSource(string RawText, string? FileName) : PreparedCvSource;

public abstract record PreparedJdSource;

public sealed record PreparedSavedJdSource(Guid JobId, string Title) : PreparedJdSource;

public sealed record PreparedRawJdSource(string RawText, string? Title) : PreparedJdSource;

/// <summary>
/// Immutable and non-ambiguous matching sources. This type is never bound from
/// an HTTP request; it is constructed only after source authorization.
/// </summary>
public sealed record PreparedMatchingRequest(
    PreparedCvSource Cv,
    PreparedJdSource Jd,
    MatchingMode Mode);

public sealed class MatchingRequestValidationResult
{
    public bool IsValid { get; init; }
    public string? FailureCode { get; init; }
    public MatchingInputSelection? Selection { get; init; }

    public static MatchingRequestValidationResult Failure(string code) => new()
    {
        IsValid = false,
        FailureCode = code
    };

    public static MatchingRequestValidationResult Success(MatchingInputSelection selection) => new()
    {
        IsValid = true,
        Selection = selection
    };
}
