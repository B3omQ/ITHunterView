using System.Collections.Generic;

namespace ITHunterview.Service.Service.Matching;

public sealed record JdMatchingEvidence(string Quotation, string Section);

public sealed record JdStageTwoItemAssessment(
    string ItemId,
    string Category,
    string HandlerCode,
    decimal Score,
    string Reasoning,
    IReadOnlyList<JdMatchingEvidence> Evidence,
    IReadOnlyList<string> DiagnosticCodes);

public enum JdStageTwoOutputQuality
{
    COMPLETE,
    PARTIAL,
    INVALID
}

public sealed record JdStageTwoOutputCoverage(
    int ExpectedCount,
    int InputCount,
    int AcceptedCount,
    int DiscardedCount,
    IReadOnlyList<string> MissingItemIds,
    bool WasTruncated);

public sealed record JdStageTwoHandlerDiagnostic(
    string Code,
    string ExpectedCategory,
    string ReturnedHandlerCode,
    string? CanonicalHandlerCode);

public sealed record JdStageTwoValidatedResponse(
    IReadOnlyDictionary<string, JdStageTwoItemAssessment> ItemAssessments,
    string Narrative,
    JdStageTwoOutputQuality Quality,
    JdStageTwoOutputCoverage Coverage,
    IReadOnlyList<string> WarningCodes)
{
    public IReadOnlyList<JdStageTwoHandlerDiagnostic> HandlerDiagnostics { get; init; }
        = Array.Empty<JdStageTwoHandlerDiagnostic>();
}
