using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching;

public interface IHardcodeCvJobPairMatcher
{
    Task<HardcodePairMatchResult> MatchAsync(
        Cvs cv,
        JobPostings job,
        CancellationToken cancellationToken = default);
}
