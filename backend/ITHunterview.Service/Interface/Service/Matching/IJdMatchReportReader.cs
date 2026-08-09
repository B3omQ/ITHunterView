using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching;

public interface IJdMatchReportReader
{
    MatchReportDto Read(string? matchDetails, decimal? persistedScore, string? matchType);
}
