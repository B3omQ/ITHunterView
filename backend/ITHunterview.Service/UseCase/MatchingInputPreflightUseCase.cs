using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase;

/// <summary>
/// Performs source authorization before feature consumption and repeats the
/// checks in a background scope before a pending match can read either source.
/// </summary>
public sealed class MatchingInputPreflightUseCase : IMatchingInputPreflightUseCase
{
    private readonly IMatchingRequestValidator _requestValidator;
    private readonly IMatchingSourceRepository _sourceRepository;

    public MatchingInputPreflightUseCase(
        IMatchingRequestValidator requestValidator,
        IMatchingSourceRepository sourceRepository)
    {
        _requestValidator = requestValidator;
        _sourceRepository = sourceRepository;
    }

    public async Task<PreparedMatchingRequest> PrepareAsync(
        Guid userId,
        MatchingRequestDto request,
        CancellationToken ct = default)
    {
        var validation = _requestValidator.Validate(request);
        if (!validation.IsValid || validation.Selection is null)
        {
            throw new ArgumentException(validation.FailureCode ?? "INVALID_MATCHING_REQUEST");
        }

        var selection = validation.Selection;
        PreparedCvSource cv;
        if (selection.CvId.HasValue)
        {
            var savedCv = await _sourceRepository.GetOwnedCvAsync(selection.CvId.Value, userId, ct);
            if (savedCv is null)
            {
                throw new KeyNotFoundException("CV not found");
            }

            cv = new PreparedSavedCvSource(savedCv.Id, savedCv.FileName);
        }
        else
        {
            cv = new PreparedRawCvSource(selection.CvText!, EmptyToNull(selection.CvFileName));
        }

        PreparedJdSource jd;
        if (selection.JobId.HasValue)
        {
            var savedJob = await _sourceRepository.GetAccessiblePublishedJobAsync(selection.JobId.Value, DateTime.UtcNow, ct);
            if (savedJob is null)
            {
                throw new KeyNotFoundException("Job not found");
            }

            jd = new PreparedSavedJdSource(savedJob.Id, savedJob.Title);
        }
        else
        {
            jd = new PreparedRawJdSource(selection.RawJdText!, EmptyToNull(selection.JdTitle));
        }

        return new PreparedMatchingRequest(cv, jd, selection.Mode);
    }

    public async Task RecheckAccessAsync(
        Guid userId,
        PreparedMatchingRequest request,
        CancellationToken ct = default)
    {
        if (request.Cv is PreparedSavedCvSource savedCv)
        {
            var cv = await _sourceRepository.GetOwnedCvAsync(savedCv.CvId, userId, ct);
            if (cv is null)
            {
                throw new KeyNotFoundException("CV not found");
            }
        }

        if (request.Jd is PreparedSavedJdSource savedJob)
        {
            var job = await _sourceRepository.GetAccessiblePublishedJobAsync(savedJob.JobId, DateTime.UtcNow, ct);
            if (job is null)
            {
                throw new KeyNotFoundException("Job not found");
            }
        }
    }

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
