using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Persistence;

public enum MatchingSourcePersistenceOutcome
{
    Persisted,
    SourceMissing,
    SourceChanged,
    AnalysisChanged,
    ActiveAnalysisInProgress
}

public interface IMatchingSourceAnalysisPersistence
{
    Task<MatchingSourcePersistenceOutcome> TryPersistCvAsync(
        CvAnalysisPersistenceIntent intent,
        CancellationToken ct = default);

    Task<MatchingSourcePersistenceOutcome> TryPersistJdAsync(
        JdAnalysisPersistenceIntent intent,
        CancellationToken ct = default);
}
