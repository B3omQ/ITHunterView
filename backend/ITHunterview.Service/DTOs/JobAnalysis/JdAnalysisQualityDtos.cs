namespace ITHunterview.Service.DTOs.JobAnalysis;

public sealed record JdAnalysisCoverage(
    int InputGroupCount,
    int AcceptedGroupCount,
    int DiscardedGroupCount,
    int InputItemCount,
    int AcceptedItemCount,
    int DiscardedItemCount,
    bool RequirementSetComplete);

public sealed record JdAnalysisDiagnostic(string Code, string JsonPath);
