using System;

namespace ITHunterview.Service.DTOs.Cv.Matching;

public sealed record MatchingInputSnapshotV1(
    string SchemaVersion,
    MatchingMode Mode,
    MatchingCvSnapshot Cv,
    MatchingJdSnapshot Jd,
    DateTime SubmittedAtUtc);

public sealed record MatchingCvSnapshot(
    string SourceKind,
    Guid? SourceId,
    string? FileName,
    string OriginalText,
    string? AnalysisJson,
    string? AnalysisSchemaVersion);

public sealed record MatchingJdSnapshot(
    string SourceKind,
    Guid? SourceId,
    string? Title,
    string OriginalText,
    string? AnalysisJson,
    string? AnalysisSchemaVersion);

public sealed record MatchingSnapshotResult(
    MatchingInputSnapshotV1 Snapshot,
    string Json,
    string Sha256);
