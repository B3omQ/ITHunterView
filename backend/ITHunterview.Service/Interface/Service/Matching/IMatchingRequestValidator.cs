using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching;

public interface IMatchingRequestValidator
{
    MatchingRequestValidationResult Validate(MatchingRequestDto request);
}
