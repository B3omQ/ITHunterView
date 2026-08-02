namespace ITHunterview.Service.DTOs.Cv.Matching;

public sealed record CvJdMatchingExecutionResult(
    decimal Score,
    string MatchDetails,
    string? SfiaExtractResult);
